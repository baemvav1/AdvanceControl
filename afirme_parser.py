#!/usr/bin/env python3
"""
Parser local de estados de cuenta AFIRME (PDF) -> XML.
Estrategia:
  1. Extraccion de texto con pdfplumber (PDF nativo, sin OCR).
  2. Maquina de estados por renglon: un movimiento inicia con "DD ... $monto [$monto]"
     y sus lineas de detalle (CUENTA:, EMISOR:, CONCEPTO:, etc.) le siguen.
  3. Desambiguacion deposito/retiro por reconstruccion de saldo corrido:
     saldo_prev + m == saldo -> deposito ; saldo_prev - m == saldo -> retiro.
  4. Validacion: cadena de saldos completa + totales vs resumen del estado.
  5. Movimientos no clasificables se marcan SIN_CLASIFICAR (hook para fallback IA).
"""
import re
import sys
import json
from decimal import Decimal
from xml.sax.saxutils import escape

import pdfplumber

MONEY = re.compile(r"\$ ?([\d,]+\.\d{2})(-?)")
ROW_START = re.compile(r"^(\d{2})\s+(.*)$")
IVA_ROW = re.compile(r"^(\d{2})\s+R\.F\.C\.\s+(\S+)\s+I\.V\.A\.\s+([\d,]*\.?\d+)\s")
COMPENSACION_FOLIO_RE = re.compile(r"^\d{10,}\s+AFIRMENET\b")

DETAIL_KEYS = [
    "CUENTA", "EMISOR", "RFC EMISOR", "CVE RASTREO", "CONCEPTO", "HORA",
    "DESTINATARIO", "RFC DESTINATARIO",
]

FOOTER_MARKERS = (
    "Método de Pago:", "Cadena Original", "Sello Digital", "Este documento",
    "Sus ahorros", "SALDO INICIAL", "Si desea recibir", "CARGOS OBJETADOS",
)


def money_to_dec(txt, neg_flag=""):
    d = Decimal(txt.replace(",", ""))
    return -d if neg_flag == "-" else d


def classify(desc):
    d = desc.upper()
    if d.startswith("SPEI RECIBIDO"):
        return "SPEI_RECIBIDO"
    if d.startswith("ENVIO SPEI"):
        return "SPEI_ENVIADO"
    if d.startswith("DEPOSITO EN EFECTIVO"):
        return "DEPOSITO_EFECTIVO"
    if d.startswith("DEPOSITO"):
        return "DEPOSITO"
    if d.startswith("COM MEMBRESIA") or d.startswith("COM ") or d.startswith("COMISION"):
        return "COMISION"
    if d.startswith("IVA"):
        return "IVA"
    if d.startswith("PAGO IMSS"):
        return "PAGO_IMSS"
    if d.startswith("PAGO IMP"):
        return "PAGO_IMPUESTOS"
    if d.startswith("PAGO VIV"):
        return "PAGO_VIVIENDA"
    if d.startswith("O.P. REC"):
        return "ORDEN_PAGO_RECIBIDA"
    if d.startswith("TRANSFERENCIA - RECEPCION"):
        return "TRANSFERENCIA_RECEPCION"
    if d.startswith("COMPENSACION SPEI") or COMPENSACION_FOLIO_RE.match(d):
        return "COMPENSACION_SPEI"
    if d.startswith("REPOSICION"):
        return "REPOSICION_TARJETA"
    return "SIN_CLASIFICAR"


def normalize_keys(blob):
    """Repara claves partidas por el salto de columna del banco (p.ej. 'CON CEPTO:')."""
    for key in sorted(DETAIL_KEYS, key=len, reverse=True):
        pat = r"\s?".join(re.escape(c) for c in key.replace(" ", ""))
        blob = re.sub(pat + r"\s?:", key + ":", blob)
    return blob


def parse_detail(lines):
    """Une lineas de detalle y separa por campos clave."""
    blob = normalize_keys(" ".join(lines))
    keys_alt = "|".join(re.escape(k) for k in sorted(DETAIL_KEYS, key=len, reverse=True))
    fields = {}
    for m in re.finditer(rf"({keys_alt}):", blob):
        start = m.end()
        nxt = re.search(rf"({keys_alt}):", blob[start:])
        end = start + nxt.start() if nxt else len(blob)
        val = blob[start:end].strip()
        k = m.group(1)
        if k in fields:
            fields[k] += " " + val
        else:
            fields[k] = val
    return fields


def extract_lines(pdf_path):
    """Texto de la zona de operaciones de todas las paginas, sin encabezados/pies."""
    out = []
    page1_text = ""
    with pdfplumber.open(pdf_path) as pdf:
        for i, page in enumerate(pdf.pages):
            txt = page.extract_text() or ""
            if i == 0:
                page1_text = txt
            in_table = False
            for line in txt.splitlines():
                if line.startswith("Día Descripción"):
                    in_table = True
                    continue
                if any(line.startswith(m) for m in FOOTER_MARKERS):
                    in_table = False
                    continue
                if in_table:
                    out.append(line.rstrip())
    return page1_text, out


def parse_header(page1):
    def grab(pat, default=""):
        m = re.search(pat, page1)
        return m.group(1).strip() if m else default

    hdr = {
        "titular": grab(r"^(.*?)\s+000010000", ) or "ADVANCE ELEVADORES DEL CARIBE SA DE CV",
        "rfc": grab(r"R\.F\.C\.\s+([A-Z0-9]{12,13})"),
        "cliente": grab(r"Número de Cliente:\s*(\d+)"),
        "cuenta": grab(r"Número de cuenta\s+(\d+)"),
        "clabe": grab(r"\(CLABE\):\s*(\d+)"),
        "folio_fiscal": grab(r"FOLIO FISCAL\s+([0-9A-F-]{36})"),
        "saldo_inicial": grab(r"Saldo inicial \$ ([\d,]+\.\d{2})"),
        "saldo_final": grab(r"Saldo al corte \$ ([\d,]+\.\d{2})"),
        "saldo_prom": grab(r"Saldo promedio diario \$ ([\d,]+\.\d{2})"),
        "saldo_min": grab(r"Saldo mínimo requerido \$ ([\d,]+\.\d{2})"),
        "depositos": grab(r"Depósitos \$ ([\d,]+\.\d{2})"),
        "retiros": grab(r"Retiros \$ ([\d,]+\.\d{2})"),
        "otras_com": grab(r"Otras comisiones \$ ([\d,]+\.\d{2})"),
        "iva_com": grab(r"IVA sobre comisiones \$ ([\d,]+\.\d{2})"),
        "total_com": grab(r"Total de comisiones \$ ([\d,]+\.\d{2})"),
        "cheques_girados": grab(r"cheques girados\s+(\d+)", "0"),
        "cheques_exentos": grab(r"cheques exentos\s+(\d+)", "0"),
        "periodo": re.search(r"Período de (\d{2})([A-Z]{3})(\d{4})AL(\d{2})([A-Z]{3})(\d{4})", page1),
    }
    return hdr


MESES = {"ENE": "01", "FEB": "02", "MAR": "03", "ABR": "04", "MAY": "05", "JUN": "06",
         "JUL": "07", "AGO": "08", "SEP": "09", "OCT": "10", "NOV": "11", "DIC": "12"}


def parse_operations(lines, saldo_inicial, year, month):
    ops = []
    cur = None
    for line in lines:
        iva_m = IVA_ROW.match(line)
        monies = list(MONEY.finditer(line))
        row_m = ROW_START.match(line)

        if iva_m and monies:
            if cur:
                ops.append(cur)
            dia = iva_m.group(1)
            cur = {
                "dia": dia, "tipo": "DESGLOSE_IVA",
                "descripcion": f"R.F.C. {iva_m.group(2)} I.V.A.",
                "rfc": iva_m.group(2),
                "iva": Decimal(iva_m.group(3).replace(",", "")),
                "monto": Decimal("0.00"),
                "saldo": money_to_dec(monies[-1].group(1), monies[-1].group(2)),
                "detalle": [],
            }
            continue

        if row_m and monies:
            if cur:
                ops.append(cur)
            dia = row_m.group(1)
            saldo = money_to_dec(monies[-1].group(1), monies[-1].group(2))
            monto = money_to_dec(monies[-2].group(1), monies[-2].group(2)) if len(monies) >= 2 else None
            head = line[: monies[0].start()].strip()
            head = re.sub(rf"^{dia}\s+", "", head)
            ref_m = re.search(r"\s(\S+)$", head)
            referencia = ""
            desc = head
            # referencia = ultimo token si es numerico corto pegado al final
            tail = head.split()
            if tail and re.fullmatch(r"[\dA-Z/.]+", tail[-1]) and re.search(r"\d", tail[-1]) and len(tail) > 1:
                # solo tokens tipo referencia (numeros/codigos), no palabras
                if re.fullmatch(r"\d+", tail[-1]) or "/" in tail[-1]:
                    referencia = tail[-1]
                    desc = " ".join(tail[:-1])
            cur = {
                "dia": dia, "tipo": classify(desc), "descripcion": desc,
                "referencia_col": referencia, "monto": monto, "saldo": saldo,
                "detalle": [],
            }
            continue

        if cur is not None and line.strip():
            cur["detalle"].append(line.strip())

    if cur:
        ops.append(cur)

    # --- Desambiguar deposito/retiro por saldo corrido y validar cadena ---
    prev = saldo_inicial
    errores = []
    for op in ops:
        m, s = op["monto"], op["saldo"]
        if op["tipo"] == "DESGLOSE_IVA" or m is None:
            op["flujo"] = "INFO"
            if s != prev:
                # los desgloses repiten el saldo vigente; si difiere hay problema
                errores.append(f"dia {op['dia']} {op['descripcion']}: saldo {s} != {prev}")
            continue
        if prev + m == s:
            op["flujo"] = "DEPOSITO"
        elif prev - m == s:
            op["flujo"] = "RETIRO"
        else:
            op["flujo"] = "NO_CUADRA"
            errores.append(f"dia {op['dia']} {op['descripcion']}: {prev} +/- {m} != {s}")
        prev = s

    # --- Enriquecer con campos de detalle ---
    for op in ops:
        f = parse_detail(op["detalle"]) if op["detalle"] else {}
        op["fields"] = f
        op["fecha"] = f"{year}-{month}-{op['dia']}"
        if op["tipo"] == "SPEI_RECIBIDO":
            op["emisor"] = f.get("EMISOR", "")
            op["rfc_emisor"] = f.get("RFC EMISOR", "")
            op["cuenta_emisor"] = f.get("CUENTA", "")
            op["concepto"] = f.get("CONCEPTO", "")
            op["cve"] = f.get("CVE RASTREO", "")
        elif op["tipo"] == "SPEI_ENVIADO":
            op["cuenta_destinatario"] = f.get("CUENTA", "")
            op["destinatario"] = re.sub(r"\(\s*D\s*A\s*T\s*O\s+NO\s+VERIFICADO[^)]*\)", "", f.get("DESTINATARIO", "")).strip()
            op["rfc_dest"] = f.get("RFC DESTINATARIO", "").split()[0] if f.get("RFC DESTINATARIO") else ""
            op["concepto"] = f.get("CONCEPTO", "")
            cve = f.get("RFC DESTINATARIO", "")
            cve_m = re.search(r"CVE RASTREO:?\s*(\S.*)", " ".join(op["detalle"]))
            op["cve"] = f.get("CVE RASTREO", "")
    return ops, errores, prev


NS_ESTADO_CUENTA = "http://www.afirme.com/estado-cuenta/v2"


def build_xml(hdr, ops, periodo_ini, periodo_fin):
    """Genera el esquema ns0:estadoCuenta que realmente lee el importador de la app
    (EsCuentaViewModel.ParsearEstadoCuentaXml) -- NO el formato oficial timbrado por
    Afirme (no hay CFDI para un PDF sin timbrar), pero con los mismos nombres y
    estructura de elementos para que el parser de la app lo reconozca."""

    def E(tag, val, ind=4):
        return f"{' ' * ind}<{tag}>{escape(str(val))}</{tag}>"

    def metadatos_block(fields, ind=8):
        limpio = {k: v for k, v in fields.items() if v not in (None, "")}
        if not limpio:
            return []
        L = [f"{' ' * ind}<ns0:metadatos>", f"{' ' * (ind + 2)}<detalle>"]
        for k, v in limpio.items():
            tag = re.sub(r"[^A-Za-z0-9]", "_", k).strip("_") or "campo"
            L.append(E(tag, v, ind + 4))
        L.append(f"{' ' * (ind + 2)}</detalle>")
        L.append(f"{' ' * ind}</ns0:metadatos>")
        return L

    L = ['<?xml version="1.0" encoding="utf-8"?>',
         f'<ns0:estadoCuenta xmlns:ns0="{NS_ESTADO_CUENTA}" version="2.1">']

    L += ["  <ns0:informacionGeneral>",
          "    <ns0:titular>",
          E("ns0:razonSocial", "ADVANCE ELEVADORES DEL CARIBE SA DE CV", 6),
          E("ns0:rfc", hdr["rfc"], 6),
          E("ns0:numeroCliente", hdr["cliente"], 6),
          "    </ns0:titular>",
          "    <ns0:cuenta>",
          E("ns0:numero", hdr["cuenta"], 6),
          E("ns0:clabe", hdr["clabe"], 6),
          E("ns0:moneda", "Moneda Nacional", 6),
          "    </ns0:cuenta>",
          "    <ns0:periodo>",
          E("ns0:fechaInicio", periodo_ini, 6),
          E("ns0:fechaFin", periodo_fin, 6),
          E("ns0:fechaCorte", periodo_fin, 6),
          "    </ns0:periodo>",
          "    <ns0:resumen>",
          E("ns0:saldoInicial", hdr["saldo_inicial"].replace(",", ""), 6),
          E("ns0:totalDepositos", hdr["depositos"].replace(",", ""), 6),
          E("ns0:totalRetiros", hdr["retiros"].replace(",", ""), 6),
          E("ns0:totalComisiones", (hdr["total_com"] or "0.00").replace(",", ""), 6),
          E("ns0:saldoFinal", hdr["saldo_final"].replace(",", ""), 6),
          E("ns0:saldoPromedio", (hdr["saldo_prom"] or "0.00").replace(",", ""), 6),
          "    </ns0:resumen>",
          "  </ns0:informacionGeneral>"]

    L.append(f'  <ns0:transacciones totalIndividuales="{len(ops)}" totalGrupos="{len(ops)}">')
    for i, o in enumerate(ops, start=1):
        L.append(f'    <ns0:grupo id="g-{i}" dia="{int(o["dia"])}" tipo="{escape(o["tipo"])}">')
        L.append("      <ns0:transaccionPrincipal>")
        L.append(E("ns0:tipo", o["tipo"], 8))
        L.append(E("ns0:descripcion", o["descripcion"], 8))

        referencia = None
        if o.get("cve"):
            referencia = f"CVE RASTREO:{o['cve']}"
        elif o.get("referencia_col"):
            referencia = o["referencia_col"]
        if referencia:
            L.append(E("ns0:referencia", referencia, 8))

        metadatos = dict(o.get("fields") or {})
        if o["tipo"] == "DESGLOSE_IVA":
            metadatos["RFC_RELACIONADO"] = o.get("rfc", "")
            metadatos["IVA"] = str(o.get("iva", ""))
        if o.get("cuenta_emisor"):
            metadatos["CUENTA_EMISOR"] = o["cuenta_emisor"]
        if o.get("cuenta_destinatario"):
            metadatos["CUENTA_DESTINATARIO"] = o["cuenta_destinatario"]
        if o.get("destinatario"):
            metadatos["DESTINATARIO"] = o["destinatario"]
        if o.get("rfc_dest"):
            metadatos["RFC_DESTINATARIO"] = o["rfc_dest"]
        L += metadatos_block(metadatos)

        L.append("        <ns0:montos>")
        L.append(E("deposito", o["monto"] if o.get("flujo") == "DEPOSITO" else "0.00", 10))
        L.append(E("retiro", o["monto"] if o.get("flujo") == "RETIRO" else "0.00", 10))
        L.append(E("saldo", o["saldo"], 10))
        L.append("        </ns0:montos>")

        L.append("      </ns0:transaccionPrincipal>")
        L.append("    </ns0:grupo>")
    L.append("  </ns0:transacciones>")

    iva_total = sum((o.get("iva") for o in ops if o["tipo"] == "DESGLOSE_IVA"), Decimal("0.00"))
    L += ["  <ns0:resumenComisiones>",
          E("ns0:iva", iva_total, 4),
          E("ns0:total", (hdr["total_com"] or "0.00").replace(",", ""), 4),
          "  </ns0:resumenComisiones>"]

    L.append("</ns0:estadoCuenta>")
    return "\n".join(L)


def build_json(hdr, ops, periodo_ini, periodo_fin):
    """Arma el request tal cual lo espera GuardarEstadoCuentaRequestDto (API real),
    sin pasar por el XML intermedio incompatible con el importador de la app."""

    def movimiento_dto(o):
        cargo = abono = None
        if o.get("flujo") == "RETIRO":
            cargo = o["monto"]
        elif o.get("flujo") == "DEPOSITO":
            abono = o["monto"]

        referencia = None
        if o.get("cve"):
            referencia = f"CVE RASTREO:{o['cve']}"
        elif o.get("referencia_col"):
            referencia = o["referencia_col"]

        metadatos = {k: v for k, v in (o.get("fields") or {}).items()}
        if o.get("cve"):
            metadatos["CVE_RASTREO"] = o["cve"]
        if o["tipo"] == "DESGLOSE_IVA":
            metadatos["RFC_RELACIONADO"] = o.get("rfc", "")
            metadatos["IVA"] = str(o.get("iva", ""))
        if o.get("cuenta_emisor"):
            metadatos["CUENTA_EMISOR"] = o["cuenta_emisor"]
        if o.get("cuenta_destinatario"):
            metadatos["CUENTA_DESTINATARIO"] = o["cuenta_destinatario"]
        if o.get("destinatario"):
            metadatos["DESTINATARIO"] = o["destinatario"]
        if o.get("rfc_dest"):
            metadatos["RFC_DESTINATARIO"] = o["rfc_dest"]
        if o.get("flujo") == "NO_CUADRA":
            metadatos["ALERTA"] = "NO_CUADRA_CONTRA_SALDO_CORRIDO"

        return {
            "fecha": o["fecha"],
            "tipo": o["tipo"],
            "subtipo": None,
            "descripcion": o["descripcion"],
            "referencia": referencia,
            "cargo": cargo,
            "abono": abono,
            "saldo": o["saldo"],
            "conciliado": False,
            "metadatos": metadatos,
            "rfc_emisor": o.get("rfc_emisor"),
        }

    grupos = []
    for i, o in enumerate(ops, start=1):
        grupos.append({
            "ordenGrupo": i,
            "grupoId": f"g-{i}",
            "dia": int(o["dia"]),
            "tipo": o["tipo"],
            "transaccionPrincipal": movimiento_dto(o),
            "movimientosRelacionados": [],
        })

    iva_total = sum((o.get("iva") for o in ops if o["tipo"] == "DESGLOSE_IVA"), Decimal("0.00"))
    iva_total += sum((o["monto"] for o in ops if o["tipo"] == "IVA" and o["monto"] is not None), Decimal("0.00"))

    return {
        "versionXml": "PDF-1.0",
        "numeroCuenta": hdr["cuenta"],
        "clabe": hdr["clabe"],
        "tipoCuenta": None,
        "tipoMoneda": "MXN",
        "fechaInicio": periodo_ini,
        "fechaFin": periodo_fin,
        "fechaCorte": periodo_fin,
        "saldoInicial": Decimal(hdr["saldo_inicial"].replace(",", "")),
        "totalCargos": Decimal(hdr["retiros"].replace(",", "")),
        "totalAbonos": Decimal(hdr["depositos"].replace(",", "")),
        "saldoFinal": Decimal(hdr["saldo_final"].replace(",", "")),
        "totalComisiones": Decimal(hdr["total_com"].replace(",", "")) if hdr["total_com"] else Decimal("0.00"),
        "totalISR": Decimal("0.00"),
        "totalIVA": iva_total,
        "totalTransaccionesIndividuales": len(ops),
        "totalGrupos": len(ops),
        "nombreBanco": "BANCA AFIRME, S.A., INSTITUCIÓN DE BANCA MÚLTIPLE",
        "rfcBanco": None,
        "nombreSucursal": None,
        "direccionSucursal": None,
        "titular": hdr["titular"],
        "rfcTitular": hdr["rfc"],
        "numeroCliente": hdr["cliente"],
        "direccionTitular": None,
        "folioFiscal": None,
        "certificadoEmisor": None,
        "fechaEmisionCert": None,
        "certificadoSat": None,
        "fechaCertificacionSat": None,
        "regimenFiscal": None,
        "metodoPago": None,
        "formaPago": None,
        "usoCfdi": None,
        "claveProdServ": None,
        "lugarExpedicion": None,
        "grupos": grupos,
    }


def _json_default(o):
    if isinstance(o, Decimal):
        return float(o)
    raise TypeError(f"No serializable: {type(o)}")


def main(pdf_path, out_path):
    page1, lines = extract_lines(pdf_path)
    hdr = parse_header(page1)
    p = hdr["periodo"]
    year, month = p.group(3), MESES[p.group(2)]
    periodo_ini = f"{p.group(3)}-{MESES[p.group(2)]}-{p.group(1)}"
    periodo_fin = f"{p.group(6)}-{MESES[p.group(5)]}-{p.group(4)}"

    saldo_ini = Decimal(hdr["saldo_inicial"].replace(",", ""))
    ops, errores, saldo_calc = parse_operations(lines, saldo_ini, year, month)

    # Validaciones globales
    dep = sum(o["monto"] for o in ops if o.get("flujo") == "DEPOSITO")
    ret = sum(o["monto"] for o in ops if o.get("flujo") == "RETIRO")
    saldo_fin = Decimal(hdr["saldo_final"].replace(",", ""))
    rep = {
        "operaciones": len(ops),
        "sin_clasificar": [o["descripcion"] for o in ops if o["tipo"] == "SIN_CLASIFICAR"],
        "errores_saldo": errores,
        "depositos_calc": str(dep), "depositos_reportados": hdr["depositos"],
        "retiros_calc": str(ret), "retiros_reportados": hdr["retiros"],
        "saldo_final_calc": str(saldo_calc), "saldo_final_reportado": str(saldo_fin),
        "saldo_cuadra": saldo_calc == saldo_fin,
        "depositos_cuadran": dep == Decimal(hdr["depositos"].replace(",", "")),
        "retiros_cuadran": ret == Decimal(hdr["retiros"].replace(",", "")),
    }
    xml = build_xml(hdr, ops, periodo_ini, periodo_fin)
    with open(out_path, "w", encoding="utf-8") as f:
        f.write(xml)

    request_dto = build_json(hdr, ops, periodo_ini, periodo_fin)
    json_path = re.sub(r"\.xml$", ".json", out_path)
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(request_dto, f, indent=2, ensure_ascii=False, default=_json_default)

    rep["numeroCuenta_dto"] = request_dto["numeroCuenta"]
    rep["clabe_dto"] = request_dto["clabe"]
    print(json.dumps(rep, indent=2, ensure_ascii=False))
    return rep


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
