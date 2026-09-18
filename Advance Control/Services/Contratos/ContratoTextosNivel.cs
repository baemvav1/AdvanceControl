namespace Advance_Control.Services.Contratos
{
    /// <summary>
    /// Texto legal fijo por nivel de contrato de suscripción, transcrito literalmente
    /// de los machotes en "Machotes contratos\Machote_{Nivel}_Modificado.md". El texto
    /// legal común a los 3 niveles vive directo en ContratoPdfService; aquí solo están
    /// los bloques que difieren entre Oro, Plata y Bronce (Cláusula Cuarta de cobertura
    /// de refacciones, Cláusula Quinta de servicios diferenciados y la mención del
    /// número de póliza en la Cláusula Novena).
    /// </summary>
    internal static class ContratoTextosNivel
    {
        internal static string ClausulaCuartaTitulo(string nivel) => nivel switch
        {
            "Bronce" => "Cláusula Cuarta: De La Cobertura de Refacciones",
            _ => "Cláusula Cuarta: Cobertura de Refacciones"
        };

        internal static string[] ClausulaCuartaParrafos(string nivel) => nivel switch
        {
            "Oro" => new[]
            {
                "Se repararán o sustituirán, a juicio de LA EMPRESA, partes o refacciones, las cuales son necesarias debido al uso y desgaste normal del equipo, y especificados acordes a cada tipo de elevador. Queda excluida la cobertura de refacciones y garantía en caso de actos de vandalismo, rescisión contractual, mal uso o uso inadecuado del equipo, interferencia de personas no pertenecientes a nuestra empresa, sobrecarga de tensión eléctrica superior a 10% de la nominal, inundaciones, temblores o terremotos y presencia de agua.",
                "No quedan incluidos en este Contrato: la parte estética de cabina, paneles, zoclos, plafones, pisos, pasamanos, espejos, intercomunicadores, tarjetas electrónicas, tableros, ventilador, sardineles de cabina, acabados ornamentales, switch de llaves y llaves, marcos, lámparas, marcos de puertas, umbrales, sardineles de puertas de pasillo, forros o pinturas de puertas tanto de carro como de pasillos, línea de alimentación del elevador, rieles y en equipos hidráulicos, la cubierta, la camisa y el pistón, válvulas y mangueras hidráulicas, cambio de aceite de la central hidráulica y enfriadores. La renovación, reparación o sustitución de las partes que a continuación se mencionan: cortinas multirrayos, displays, botones y botoneras de cabina y piso, cambio de motor o estator de corriente alterna, máquina de tracción con y/o sin engranaje, cables de tracción, regulador de velocidad, reparación de jaula de ardilla de motor de corriente alterna, poleas y bandas tractoras y deflectoras, bobina y tambor de freno, corona de tracción y sinfín, drive, reparación del moto-generador, placa de circuito impreso micro-procesado, sistema de monitoreo y programación de tráfico.",
                "Para escaleras, no quedan incluidas las siguientes partes: pasamanos, cadenas tractoras, cadenas viajeras, carro tensor, escalones, engranajes, máquina de tracción, acabados ornamentales, placas de desembarque superior e inferior, drives, balaustradas (acero, vidrio, etc.) y demarcaciones de escalones."
            },
            "Plata" => new[]
            {
                "Se repararán o sustituirán, a juicio de LA EMPRESA, partes o refacciones, las cuales son necesarias debido al uso y desgaste normal del equipo, y especificados acordes a cada tipo de elevador. Queda excluida la cobertura de refacciones y garantía en caso de actos de vandalismo, rescisión contractual, mal uso o uso inadecuado del equipo, interferencia de personas no pertenecientes a nuestra empresa, sobrecarga de tensión eléctrica superior a 10% de la nominal, inundaciones y temblores, terremotos y presencia de agua.",
                "No incluye: la parte estética de cabina, plafones, pisos, pasamanos, espejos, intercomunicadores, tableros, tarjetas electrónicas, ventilador, sardineles de cabina, acabados ornamentales, switch de llaves y llaves, marcos, lámparas, marcos de puertas, cortinas multirrayos, umbrales, sardineles de puertas de pasillo, forros o pinturas de puertas tanto de carro como de pasillos, línea de alimentación del elevador y rieles. Y en equipos hidráulicos: la cubierta, la camisa y el pistón, válvulas y mangueras hidráulicas, cambio de aceite de la central hidráulica y enfriadores. La renovación, reparación o sustitución de las partes que a continuación se mencionan, no quedan incluidas en este Contrato: cambio de motor o estator de corriente alterna, máquina de tracción con y/o sin engranaje, cables de tracción, regulador de velocidad, reparación de jaula de ardilla de motor de corriente alterna, poleas tractoras y deflectoras, bobina y tambor de freno, corona de tracción y sinfín, drive, reparación del moto-generador, placa de circuito impreso micro-procesado, sistema de monitoreo y programación de tráfico.",
                "Para escaleras, no quedan incluidas las siguientes partes: pasamanos, rodamientos de pasamanos, newells de retorno, cadenas tractoras, viajeras y de pasamanos, carro tensor, motor, escalones, engranajes, máquina de tracción, acabados ornamentales, placas de desembarque superior e inferior, drives, balaustradas (acero, vidrio, etc.) y demarcaciones de escalones."
            },
            _ => new[]
            {
                "Se repararán o sustituirán a juicio de LA EMPRESA y por cuenta de EL CLIENTE, partes o refacciones, las cuales son necesarias debido al uso y desgaste normal del equipo, y especificados acordes a cada tipo de elevador. Materiales contemplados en el Contrato Bronce:",
                "Fusibles de Vidrio, Relleno de Aceite de Máquina (no incluye el cambio de aceite), Lubricantes en Rieles y en Cables Tractores, y Material de Limpieza."
            }
        };

        internal static string ClausulaQuintaTitulo(string nivel) => nivel switch
        {
            "Bronce" => "Cláusula Quinta: De Los Servicios Diferenciados",
            "Plata" => "Cláusula Quinta: De los Servicios Diferenciados",
            _ => "Cláusula Quinta: De los Servicios Diferenciados"
        };

        /// <summary>Bloques (subtítulo, párrafo) de la Cláusula Quinta. Oro y Plata incluyen "Conferencias Educativas Gratuitas"; Bronce no.</summary>
        internal static (string Subtitulo, string Parrafo)[] ClausulaQuintaBloques(string nivel)
        {
            var atencionLlamadas = (
                "Atención a Llamadas 24 horas de los 365 días del año",
                "LA EMPRESA garantiza una rápida atención a los clientes, a través de una moderna flotilla de vehículos. Atención de llamadas de emergencia 24 horas a través de radios Nextel.");

            var conferencias = (
                "Conferencias Educativas Gratuitas",
                "Advance Elevadores Acapulco tiene como principio el mejoramiento continuo de la calidad de servicio y la valoración de la relación con nuestros clientes. Basados en esta premisa, ofrecemos conferencias gratuitas para Administradores, Personal de Ingeniería, Residentes de Condominios, Empleados y Clientes; presentamos temas sobre riesgos de rescate de pasajeros, la mejor forma de utilizar y administrar el uso y conservación de elevadores, aceras móviles y escaleras.");

            var inspeccionOro = (
                "Inspección Anual de Desempeño y Seguridad (RIA – Reporte de Inspección Anual)",
                "Para garantizar la seguridad de nuestros clientes, LA EMPRESA incluye en este Contrato de manera exclusiva, pruebas a cada 12 meses de todos los mecanismos de puertas de cabina y pasillo, dispositivo de regulador de velocidad y freno de seguridad en la cabina, cables y poleas de tracción, límites de final de curso, dispositivos de emergencia y protección a usuarios y técnicos, circuitos eléctricos de protección. Como resultado de esta inspección entregamos un RIA firmado por el Ingeniero especialista en Transporte Vertical.");

            var inspeccionPlataBronce = (
                "Inspección Anual de Desempeño y Seguridad (RIA – Reporte de Inspección Anual)",
                "Para garantizar la seguridad de nuestros clientes, LA EMPRESA incluye en este Contrato de manera exclusiva, pruebas a cada 12 meses de todos los mecanismos de puertas de cabina y pasillo, dispositivo de regulador de velocidad y freno de seguridad en la cabina, cables y poleas de tracción, límites de final de curso, dispositivos de emergencia, protección a usuarios y técnicos, y circuitos eléctricos de protección. Como resultado de esta inspección entregamos un RIA firmado por el Ingeniero especialista en Transporte Vertical.");

            var refacciones = (
                "Refacciones y Componentes Originales",
                "LA EMPRESA posee una central de refacciones y componentes con más de 1500 productos disponibles para reposición de piezas en elevadores y escaleras, incluso modelos más antiguos, a manera de atender a nuestros clientes con rapidez y eficacia. Adquirimos e importamos repuestos de los propios fabricantes.");

            var refaccionesPlazoOro = (
                "",
                "Se utilizarán piezas originales para reposición en un plazo máximo de 5 días, dependiendo de la pieza o refacción a substituir, cumpliendo los altos estándares de Calidad para todos los tipos de elevadores, escaleras y aceras móviles.");

            var sinMulta = (
                "Sin Multa por Cancelación del Contrato",
                "LA EMPRESA, segura de rebasar los estándares de calidad en el servicio proporcionado, y en consecuencia, alcanzando la satisfacción total de EL CLIENTE, no penalizará la cancelación del presente Contrato, previo aviso por escrito con 60 días de anticipación y previa notificación de recibido.");

            return nivel switch
            {
                "Oro" => new[] { atencionLlamadas, conferencias, inspeccionOro, refacciones, refaccionesPlazoOro, sinMulta },
                "Plata" => new[] { atencionLlamadas, conferencias, inspeccionPlataBronce, refacciones, refaccionesPlazoOro, sinMulta },
                _ => new[] { atencionLlamadas, inspeccionPlataBronce, refacciones, sinMulta }
            };
        }

        /// <summary>Cláusula Novena: en Bronce se menciona el número de póliza; en Oro/Plata no.</summary>
        internal static string ClausulaNovenaTexto(string nivel)
        {
            const string comun1 = "LA EMPRESA no se responsabilizará por daños y perjuicios a terceros en sus bienes o en sus personas, excepto cuando se deriven de actos u omisiones atribuibles directamente a LA EMPRESA o los que causen sus trabajadores en ejercicio de sus funciones, para lo cual se hará uso de la Póliza de Responsabilidad Civil";

            var poliza = nivel == "Bronce"
                ? " con número 10202 30045335,"
                : "";

            const string comun2 = " que es hasta por $2'000,000.00 (DOS MILLONES DE PESOS 00/100) MN, para eventuales indemnizaciones por daños personales y/o materiales recurrentes de los servicios prestados por LA EMPRESA. En ningún caso será LA EMPRESA responsable por daños consecuentes y por casos fortuitos y/o de fuerza mayor como: incendios, explosiones, inundaciones, robos, temblores, uso indebido del equipo, manipulación de los mismos por terceros, incluyendo huelga o conflictos de carácter laboral.";

            return comun1 + poliza + comun2;
        }

        internal const string FolioRepse = "Folio REPSE número: 9cafdb31-6fef-4f70-bf38-d98c13c3bdaa";
    }
}
