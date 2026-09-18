using Advance_Control.Models;
using Advance_Control.Services.Logging;
using Advance_Control.Utilities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Advance_Control.Services.Contratos
{
    /// <summary>
    /// Genera el PDF de un contrato de suscripción (Oro/Plata/Bronce) usando QuestPDF,
    /// reproduciendo el texto legal exacto de los machotes en
    /// "Machotes contratos\Machote_{Nivel}_Modificado.md". El texto legal es fijo;
    /// solo se interpolan los campos variables (cliente, equipos, montos, vigencia, firmante).
    /// </summary>
    public class ContratoPdfService : IContratoPdfService
    {
        private static readonly CultureInfo EsMx = new CultureInfo("es-MX");
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private readonly ILoggingService _logger;

        public ContratoPdfService(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private static string GetContratosFolder()
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var folder = Path.Combine(documentsPath, "Advance Control", "Contratos");
            Directory.CreateDirectory(folder);
            return folder;
        }

        public async Task<string> GenerarContratoPdfAsync(
            ContratoSuscripcionDto contrato,
            string clienteRazonSocial,
            List<EquipoDto> equiposCubiertos)
        {
            if (contrato == null) throw new ArgumentNullException(nameof(contrato));

            await _logger.LogInformationAsync(
                $"Generando PDF de contrato de suscripción {contrato.Id} ({contrato.Nivel})",
                "ContratoPdfService", "GenerarContratoPdfAsync");

            var nivel = contrato.Nivel;
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var fileName = $"Contrato_{contrato.Id}_{fecha}.pdf";
            var filePath = Path.Combine(GetContratosFolder(), fileName);

            var montoTexto = $"$ {contrato.MontoMensual.ToString("N2", UsCulture)} ({NumeroALetrasHelper.ConvertirMontoALetras(contrato.MontoMensual)}) Más I.V.A.";
            var unidadesTexto = $"{contrato.NumeroUnidades} ({NumeroALetrasHelper.ConvertirUnidadesALetras(contrato.NumeroUnidades)})";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9.5f));

                    page.Header().ShowOnce().Column(column =>
                    {
                        column.Item().Text("ADVANCE ELEVADORES ACAPULCO, S.A. DE C.V.").Bold().FontSize(13);
                        column.Item().Text("RFC: AEA110715MPA – TIP: B61 67831 10").Bold();
                        column.Item().Text("Río Papagayo No. 36 Dep. A, Col. Vista Alegre, C.P. 39560. Acapulco, Gro.");
                        column.Item().Text("Tel. (744) 273 – 5241");
                        column.Item().Text("Sucursal CDMX Lábaro Patrio 13 Dep. 304, Col. Lomas del Chamizal. C.P. 05129");
                        column.Item().PaddingTop(8).AlignCenter().Text("CONTRATO DE MANTENIMIENTO").FontSize(13).Bold();
                        column.Item().AlignCenter().Text($"ADVANCE {nivel?.ToUpperInvariant()}").FontSize(13).Bold();
                    });

                    page.Content().PaddingVertical(0.5f, Unit.Centimetre).Column(column =>
                    {
                        column.Spacing(6);

                        // ── INCLUYE ──────────────────────────────────────────
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                            table.Header(h =>
                            {
                                h.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten2).Padding(4).Text("INCLUYE:").Bold();
                            });
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Atención a Llamadas 24 horas, 365 días al año");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Seguro de Responsabilidad Civil");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Refacciones y Componentes Originales");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Inspección Anual de Desempeño y Seguridad");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Sin Multa Contractual por Cancelación");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Folio REPSE vigente");
                        });

                        // ── Datos del cliente / dirección / número de contrato ──
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(col =>
                        {
                            col.Item().Text(t => { t.Span("Cliente / Razón Social: ").Bold(); t.Span(clienteRazonSocial); });
                            col.Item().PaddingTop(3).Text(t => { t.Span("Dirección - Instalación: ").Bold(); t.Span(contrato.DireccionInstalacion ?? ""); });
                            col.Item().PaddingTop(3).Text(t => { t.Span("Número de Contrato: ").Bold(); t.Span(contrato.NumeroContrato ?? ""); });
                        });

                        // ── Tabla de equipos ─────────────────────────────────
                        column.Item().PaddingTop(4).Text("CARACTERÍSTICAS TÉCNICAS DE LOS EQUIPOS").Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);  // Unidad
                                c.RelativeColumn(1.4f); // Marca
                                c.RelativeColumn(1.4f); // Controlador
                                c.RelativeColumn(0.8f); // Par/Des
                                c.RelativeColumn(1.2f); // Tipo Puerta
                                c.RelativeColumn(1);    // Velocidad
                                c.RelativeColumn(1.3f); // Tipo Máquina
                                c.RelativeColumn(1.1f); // Operador
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Unidad").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Marca").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Controlador").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Par/Des").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Tipo Puerta").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Velocidad").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Tipo Máquina").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("Operador").Bold().FontSize(8);
                            });

                            var n = 1;
                            foreach (var eq in equiposCubiertos)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(n.ToString()).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(eq.Marca ?? "").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(eq.Controlador ?? "").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(eq.Paradas?.ToString() ?? "").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(eq.TipoPuerta ?? "").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(eq.Velocidad ?? "").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(eq.TipoMaquina ?? "").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(eq.Operador ?? "").FontSize(8);
                                n++;
                            }
                        });

                        // ── Condiciones contractuales ────────────────────────
                        column.Item().PaddingTop(4).Text("CONDICIONES CONTRACTUALES").Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(3); });
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Valor Mensual Contratado").Bold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(montoTexto).Bold();
                        });
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(3); });
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Vigencia").Bold();
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4)
                                .Text($"12 meses, del {contrato.VigenciaInicio:dd 'de' MMMM 'de' yyyy} al {contrato.VigenciaFin:dd 'de' MMMM 'de' yyyy}");
                        });

                        // ── Horarios ─────────────────────────────────────────
                        column.Item().PaddingTop(4).Text("HORARIOS DE ATENCIÓN").Bold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(2); });
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Mantenimiento Preventivo y Correctivo");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("De 9:00hrs a 18:00hrs, Lunes a Viernes");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Atención en oficinas");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("De 9:00hrs a 18:00hrs, Lunes a Viernes");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("Atención a llamadas de emergencia");
                            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Text("24 horas, 365 días del año");
                        });

                        // ── Intro ────────────────────────────────────────────
                        column.Item().PaddingTop(6).Text(t =>
                        {
                            t.Justify();
                            t.Span("CONTRATO DE MANTENIMIENTO, QUE CELEBRAN POR UNA PARTE ").Bold();
                            t.Span("ADVANCE ELEVADORES ACAPULCO, S.A. DE C.V.,").Bold().Italic();
                            t.Span(" (EN LO SUCESIVO “LA EMPRESA”), Y POR OTRA PARTE ").Bold();
                            t.Span(clienteRazonSocial.ToUpperInvariant()).Bold().Italic();
                            t.Span(" (EN LO SUCESIVO “EL CLIENTE”), A QUIEN JUNTO CON “LA EMPRESA” EN LO SUCESIVO SE LES DENOMINARA “LAS PARTES”, AL TENOR DE LAS SIGUIENTES:").Bold();
                        });

                        column.Item().PaddingTop(4).AlignCenter().Text("CLÁUSULAS CONTRACTUALES").Bold().FontSize(11);

                        // ── Cláusula Primera ─────────────────────────────────
                        AddClausula(column, "Cláusula Primera: De El Pago",
                            $"Como contraprestación del presente contrato para el mantenimiento preventivo mensual de {unidadesTexto} unidad(es), EL CLIENTE se obliga a cubrir a LA EMPRESA la cantidad de: {montoTexto}, el cual será pagado en 12 (doce) exhibiciones mensuales iguales y consecutivas. Los pagos deberán ser efectuados durante los primeros cinco días hábiles de cada mes posteriores a la fecha de vencimiento del pago, mediante transferencia electrónica o depósito bancario en la siguiente cuenta:");
                        column.Item().Text(t =>
                        {
                            t.Span("BANCO: BBVA BANCOMER    NOMBRE: ADVANCE ELEVADORES ACAPULCO, SA DE CV\n").Bold();
                            t.Span("CLABE INTERBANCARIA: 012261001859468328    CUENTA: 0185946832\n").Bold();
                            t.Span("SUCURSAL: 0580 – ACAPULCO CONVENCIONES").Bold();
                        });
                        AddParrafo(column,
                            "Cualquiera de los pagos acordados no efectuados en la fecha del vencimiento indicado en las facturas, causará un interés moratorio conforme a las prácticas de mercado, y en ningún caso será menor al Costo Porcentual Promedio de Captación de Dinero (Tasa TIIE) que fije el Banco de México, más 10 puntos porcentuales vigentes durante el tiempo que dure la moratoria.");
                        AddParrafo(column,
                            "Para el caso de que EL CLIENTE no efectúe los pagos a que se ha obligado en los términos convenidos, LA EMPRESA podrá exigir el cumplimiento forzoso o bien rescindir el presente Contrato sin menoscabo del pago de las mensualidades acumuladas al momento de la rescisión. Asimismo, LA EMPRESA podrá suspender el mantenimiento preventivo, correctivo y la atención de llamadas de emergencia, argumentando el incumplimiento de aquel, por lo que durante todo el tiempo de la suspensión del servicio objeto del contrato imputable a EL CLIENTE, LA EMPRESA no será responsable de ningún aspecto técnico, legal y civil, quedando relevado de sus obligaciones por el sólo incumplimiento de EL CLIENTE.");

                        // ── Cláusula Segunda ─────────────────────────────────
                        AddClausula(column, "Cláusula Segunda: De la Vigencia y Reajuste de Precio",
                            "El presente Contrato surtirá efectos a partir de lo estipulado en las CONDICIONES CONTRACTUALES y tendrá una vigencia de 12 (doce) meses, siendo renovado al término de la vigencia, en forma automática y por tiempo indeterminado. El precio se incrementará anualmente en el mismo porcentaje que se incremente el índice de precios al consumidor que publique el Diario Oficial de la Federación, hasta que cualquiera de LAS PARTES pretenda darlo por terminado, en cuyo caso notificará su voluntad en forma fehaciente a la otra parte por escrito con 60 días de anticipación y obteniendo respuesta de recibida y aceptada la notificación.");
                        AddParrafo(column,
                            "El precio del presente Contrato está basado en los costos de materiales nacionales y de importación, así como de la mano de obra vigentes a la fecha de elaboración de este instrumento; por tanto, en caso de existir cambios en los factores mencionados, el importe estará sujeto a los cambios correspondientes, los cuales LA EMPRESA se obliga a notificar por escrito a EL CLIENTE con 30 días de anticipación.");

                        // ── Cláusula Tercera ─────────────────────────────────
                        AddClausula(column, "Cláusula Tercera: De Garantía",
                            "Los materiales mecánicos instalados por LA EMPRESA tienen garantía por un período de 6 (seis) meses, contados a partir de la fecha de instalación y aprobación de los servicios a través de la firma de aceptación de EL CLIENTE, siempre y cuando se encuentre vigente el Contrato.");
                        AddParrafo(column, "Los servicios de mano de obra están garantizados durante el tiempo de vigencia del presente Contrato.");

                        // ── Cláusula Cuarta (por nivel) ──────────────────────
                        var nivelKey = nivel ?? "Bronce";
                        var parrafosCuarta = ContratoTextosNivel.ClausulaCuartaParrafos(nivelKey);
                        AddClausula(column, ContratoTextosNivel.ClausulaCuartaTitulo(nivelKey), parrafosCuarta[0]);
                        for (var i = 1; i < parrafosCuarta.Length; i++)
                            AddParrafo(column, parrafosCuarta[i]);

                        // ── Cláusula Quinta (por nivel) ───────────────────────
                        column.Item().PaddingTop(6).Text(ContratoTextosNivel.ClausulaQuintaTitulo(nivelKey)).Bold();
                        foreach (var (subtitulo, parrafo) in ContratoTextosNivel.ClausulaQuintaBloques(nivelKey))
                        {
                            if (!string.IsNullOrEmpty(subtitulo))
                                column.Item().PaddingTop(3).Text(subtitulo).Bold();
                            column.Item().PaddingTop(subtitulo == "" ? 0 : 1).Text(parrafo).Justify();
                        }

                        // ── Cláusula Sexta ────────────────────────────────────
                        AddClausula(column, "Cláusula Sexta: De la Mano de Obra",
                            "Todos los trabajos aquí estipulados son efectuados por técnicos especializados, debidamente capacitados, entrenados y certificados, que cuentan con uniforme e identificación de LA EMPRESA, que los acredita como miembros de la misma y con herramienta adecuada para cada tipo de tecnología.");

                        // ── Cláusula Séptima (lista numerada) ────────────────
                        column.Item().PaddingTop(6).Text("Cláusula Séptima: Condiciones Generales").Bold();
                        AddListaNumerada(column, ClausulaSeptimaItems);

                        // ── Cláusula Octava (lista numerada) ─────────────────
                        column.Item().PaddingTop(6).Text("Cláusula Octava: Obligaciones de EL CLIENTE").Bold();
                        AddListaNumerada(column, ClausulaOctavaItems);

                        // ── Cláusula Novena (por nivel) ───────────────────────
                        AddClausula(column, "Cláusula Novena: De La Responsabilidad Civil y REPSE",
                            ContratoTextosNivel.ClausulaNovenaTexto(nivelKey));
                        column.Item().Text(ContratoTextosNivel.FolioRepse).Bold();

                        // ── Cláusula Décima ───────────────────────────────────
                        AddClausula(column, "Cláusula Décima: Responsabilidad Laboral",
                            "LA EMPRESA reconoce y acepta ser el único patrón de todas y cada una de las personas que intervengan en el desarrollo y ejecución de los servicios pactados en este Contrato, liberando a EL CLIENTE de cualquier responsabilidad laboral.");

                        // ── Cláusula Décima Primera ───────────────────────────
                        AddClausula(column, "Cláusula Décima Primera: Jurisdicción",
                            "Para la interpretación y cumplimiento del presente Contrato, ambas partes se someten a la legislación mercantil y a la competencia de los tribunales del fuero común de la Ciudad y Puerto de Acapulco, Guerrero, renunciando expresamente a cualquier fuero que por su domicilio pudiera corresponderle.");

                        // ── Firma ──────────────────────────────────────────────
                        var fechaFirma = contrato.FechaFirma ?? DateTime.Now;
                        column.Item().PaddingTop(10).Text(
                            $"EL PRESENTE CONTRATO se firma por duplicado en la Ciudad y Puerto de Acapulco, Guerrero, a los {fechaFirma.Day} días del mes de {fechaFirma.ToString("MMMM", EsMx)} de {fechaFirma.Year}.")
                            .Bold();

                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("LA EMPRESA").Bold();
                                col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Black);
                                col.Item().PaddingTop(2).Text("Advance Elevadores Acapulco, SA de CV").Bold();
                                col.Item().Text("Lic. Antonio Espeja García,").Bold();
                                col.Item().Text("Director General y Representante Legal").Bold();
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("EL CLIENTE").Bold();
                                col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Black);
                                col.Item().PaddingTop(2).Text(t => { t.Span("Nombre: ").Bold(); t.Span(contrato.NombreFirmante ?? ""); });
                                col.Item().Text(t => { t.Span("Teléfono: ").Bold(); t.Span(contrato.TelefonoFirmante ?? ""); });
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span($"Contrato de Suscripción {nivel} — {contrato.NumeroContrato}, Hoja ");
                        x.CurrentPageNumber();
                        x.Span("/");
                        x.TotalPages();
                    });
                });
            });

            document.GeneratePdf(filePath);

            await _logger.LogInformationAsync($"PDF de contrato de suscripción generado: {filePath}", "ContratoPdfService", "GenerarContratoPdfAsync");

            return filePath;
        }

        private static void AddClausula(QuestPDF.Fluent.ColumnDescriptor column, string titulo, string parrafo)
        {
            column.Item().PaddingTop(6).Text(titulo).Bold();
            column.Item().PaddingTop(1).Text(parrafo).Justify();
        }

        private static void AddParrafo(QuestPDF.Fluent.ColumnDescriptor column, string parrafo)
        {
            column.Item().PaddingTop(3).Text(parrafo).Justify();
        }

        private static void AddListaNumerada(QuestPDF.Fluent.ColumnDescriptor column, string[] items)
        {
            for (var i = 0; i < items.Length; i++)
            {
                var index = i + 1;
                column.Item().PaddingTop(2).Row(row =>
                {
                    row.ConstantItem(18).Text($"{index}.").Bold();
                    row.RelativeItem().Text(items[i]).Justify();
                });
            }
        }

        private static readonly string[] ClausulaSeptimaItems =
        {
            "Este servicio se realiza en los horarios indicados en las condiciones contractuales.",
            "El mantenimiento se proporciona mediante visitas MENSUALES, de acuerdo a un sistema de mantenimiento preventivo, programado y controlado, ejecutado por nuestros técnicos y supervisado por personal altamente calificado.",
            "Por cada una de las visitas de los técnicos, EL CLIENTE o su representante del inmueble deberá firmar de conformidad un comprobante de visita, en el que se especificará el tipo de servicio realizado, y cuyo original es entregado en el mismo momento a EL CLIENTE o representante, conservando LA EMPRESA la copia fiel.",
            "Se realizan los ajustes menores necesarios y se proporciona una limpieza y lubricación en las partes mecánicas y eléctricas, con grasas, aceites y lubricantes especiales, de acuerdo a especificaciones técnicas. Las refacciones o partes sustituidas que no sea posible su reparación serán devueltas a LA EMPRESA para su adecuada destrucción y desecho de acuerdo a normas ambientales y de seguridad.",
            "En el caso de cambios de diseños y avances tecnológicos, algunas refacciones pueden ser descontinuadas, dificultando el mantenimiento y la operación eficiente del equipo. Cuando ocurra lo anterior, LA EMPRESA notificará por escrito a EL CLIENTE los cambios que se recomiendan al equipo, así como el costo correspondiente. Las modificaciones deberán autorizarse dentro del término de 12 (doce) meses contratados a partir de la fecha de notificación a que se refiere el punto anterior, en caso contrario, LA EMPRESA y EL CLIENTE acordarán las modificaciones pertinentes al Contrato, que excluirán los componentes que no pueden mantenerse eficientemente por obsolescencia de refacciones, acordando de esta manera el monto correspondiente al Contrato.",
            "La atención de llamadas de emergencia se proporcionará de acuerdo a lo indicado en las condiciones contractuales, siendo responsabilidad de LA EMPRESA contar con una plantilla de técnicos especializados para atención de las mismas, coordinados a través del Centro de Atención a Clientes.",
            "El acceso a la parte superior de la cabina, al foso y cualquier otra área del cubo está restringido a personal capacitado y autorizado por LA EMPRESA, dados los riesgos de sufrir accidentes. Por lo anterior, la llave de apertura de puertas se le entregará a EL CLIENTE sólo a solicitud expresa y por escrito, acompañada de una carta responsiva sobre cualquier accidente ocurrido a personal de EL CLIENTE o público usuario, ocurrido por el uso inapropiado de la misma.",
            "LA EMPRESA no está obligada a hacer reposiciones o reparaciones, derivadas del uso indebido del equipo, negligencia del usuario, sobrecargas, daños intencionales causados por EL CLIENTE o por terceras personas, fuera del control de LA EMPRESA y no imputables a ésta.",
            "LA EMPRESA no se hace responsable del mal funcionamiento de los equipos, provocados por casos fortuitos como: huelga, problemas causados por actos de vandalismo, filtraciones de agua, falta de tierra física, sobrecarga de los equipos por arriba de lo permitido, suministro de energía eléctrica, defectuosa construcción civil del inmueble, incendios dentro del inmueble, piezas o trabajos realizados por personal ajeno a LA EMPRESA. Todo esto dará como resultado la pérdida de la garantía del servicio y sin responsabilidad y cargo alguno para LA EMPRESA.",
            "Cuando ocurra alguno de los siniestros y eventos mencionados en el punto anterior y el equipo quede sin funcionar, LA EMPRESA se compromete a presentar a EL CLIENTE, el presupuesto correspondiente de los trabajos requeridos para la solución del problema.",
            "Este Contrato no ampara inspecciones de seguridad ni instalación de dispositivos nuevos al equipo que sean recomendados o exigidos por compañías de seguros o por las autoridades competentes y en consecuencia, no obliga a LA EMPRESA a efectuarlas."
        };

        private static readonly string[] ClausulaOctavaItems =
        {
            "El CLIENTE se obliga a brindar al personal técnico de LA EMPRESA, un acceso seguro al cuarto de máquinas del elevador, así como instalar una escalera marina para ingresar al foso del mismo, cuando la profundidad sea de 90 centímetros o más.",
            "EL CLIENTE se obliga a proporcionar iluminación adecuada y suficiente al cuarto de máquinas.",
            "No permitir el acceso a personas ajenas a LA EMPRESA al cuarto de máquinas, al cubo del elevador, al foso o la parte superior de la cabina.",
            "No permitir que se almacenen objetos ajenos a las instalaciones (no usarlo como bodega) y reparará por su cuenta cualquier deterioro de obra civil del inmueble que afecte el funcionamiento normal del equipo.",
            "Autorizar expresamente al personal designado por LA EMPRESA a desmontar partes del equipo y llevarlos a sus talleres, con el fin de someterlos a pruebas, inspecciones, reparaciones, con previo aviso a EL CLIENTE o a su representante.",
            "El CLIENTE está de acuerdo en proporcionar a LA EMPRESA, copia de su registro federal de contribuyentes en cumplimiento a las disposiciones fiscales vigentes y copia de su acta constitutiva en caso de que se trate de una persona moral, así como comprobante de domicilio, poder notarial del representante legal, acta del comité de condóminos y/o documento donde se designa la representación legal del administrador del inmueble."
        };
    }
}
