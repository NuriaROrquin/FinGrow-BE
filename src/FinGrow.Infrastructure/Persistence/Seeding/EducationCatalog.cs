namespace FinGrow.Infrastructure.Persistence.Seeding;

using FinGrow.Domain.Enums;

internal static class EducationCatalog
{
    public static readonly CourseSeed[] Courses =
    {
        new(
            "fundamentos-finanzas-personales",
            "Fundamentos de Finanzas Personales",
            "Aprendé a gestionar tu dinero, armar un presupuesto y ahorrar de forma sostenida.",
            CourseLevel.Beginner,
            EducationCategory.Basics,
            null,
            new LessonSeed[]
            {
                new("Introducción a las finanzas personales", 10, "https://www.youtube.com/embed/9sCVcWD1Svs"),
                new("Cómo crear un presupuesto efectivo", 15, "https://www.youtube.com/embed/B66NtYRkhPA"),
                new("Estrategias de ahorro", 12, "https://www.youtube.com/embed/U5wCPaNAjls"),
            }),
        new(
            "estrategias-de-inversion-101",
            "Estrategias de Inversión 101",
            "Entendé acciones, bonos y ETFs, y cómo construir un portafolio diversificado.",
            CourseLevel.Intermediate,
            EducationCategory.Investments,
            InvestmentType.Etf,
            new LessonSeed[]
            {
                new("Introducción a la inversión", 15, "https://www.youtube.com/embed/I_cD2SEdWAA"),
                new("Tipos de activos financieros", 20, "https://www.youtube.com/embed/c27c0cgcNAo"),
                new("Diversificación de portafolio", 18, "https://www.youtube.com/embed/1bc5O3sJRYQ"),
            }),
        new(
            "gestion-de-deudas-y-credito",
            "Gestión de Deudas y Crédito",
            "Dominá tu historial crediticio, las estrategias para salir de deudas y el uso de tarjetas.",
            CourseLevel.Beginner,
            EducationCategory.Credit,
            null,
            new LessonSeed[]
            {
                new("Entendiendo el crédito", 12, "https://www.youtube.com/embed/k2eKBaHu4dc"),
                new("Mejorando tu puntaje crediticio", 15, "https://www.youtube.com/embed/40cN-fJawKk"),
                new("Estrategias para pagar deudas", 18, "https://www.youtube.com/embed/JklsK3Sw8b4"),
            }),
        new(
            "planificacion-de-jubilacion",
            "Planificación de Jubilación",
            "Planificá tu futuro con estrategias de ahorro e inversión para el retiro.",
            CourseLevel.Advanced,
            EducationCategory.Retirement,
            null,
            new LessonSeed[]
            {
                new("Importancia del ahorro para el retiro", 10, "https://www.youtube.com/embed/AIYHpYqP-tA"),
                new("Cuentas de jubilación", 20, "https://www.youtube.com/embed/qstxpeYzsng"),
                new("Calculando tus necesidades de jubilación", 15, "https://www.youtube.com/embed/ZdB3nv8l2GY"),
            }),
    };

    public static readonly ArticleSeed[] Articles =
    {
        new(
            "fondo-de-emergencia",
            "5 formas de construir un fondo de emergencia",
            "Estrategias concretas para juntar un colchón de 3 a 6 meses de gastos sin endeudarte.",
            EducationCategory.Savings,
            4,
            null,
            """
            Un fondo de emergencia te protege de imprevistos —una pérdida de empleo, un gasto médico,
            una reparación— sin tener que recurrir a deudas caras.

            ## 1. Automatizá el ahorro
            Programá una transferencia a una cuenta separada el mismo día que cobrás. Tratalo como un
            gasto fijo más.

            ## 2. Fijate un objetivo en meses de gastos
            La referencia habitual es cubrir **entre 3 y 6 meses de gastos esenciales**.

            ## 3. Usá los ingresos extra
            Aguinaldo, bonos o devoluciones de impuestos son la forma más rápida de avanzar.

            ## 4. Recortá gastos recurrentes
            Revisá suscripciones y servicios duplicados y redirigí esa plata al fondo.

            ## 5. Avanzá por etapas
            Empezá por un mes de gastos. Cada meta cumplida refuerza el hábito.
            """),
        new(
            "interes-compuesto",
            "Entendiendo el interés compuesto",
            "Por qué empezar a invertir temprano pesa más que la cantidad que invertís.",
            EducationCategory.Investments,
            3,
            InvestmentType.MutualFund,
            """
            El interés compuesto es ganar intereses **sobre los intereses** que ya ganaste.

            ## Un ejemplo
            Si invertís $100.000 al 10% anual y reinvertís lo ganado, al año tenés $110.000; al
            segundo, $121.000; al décimo, más de $259.000.

            ## Qué lo hace crecer
            - **Tiempo**: es la variable que más pesa.
            - **Tasa**: una diferencia chica se multiplica con los años.
            - **Constancia**: aportar todos los meses acelera el efecto.

            Los fondos comunes de inversión reinvierten automáticamente, por eso son una forma
            simple de aprovecharlo.
            """),
        new(
            "presupuesto-mensual",
            "Cómo crear un presupuesto mensual",
            "Un método paso a paso para saber a dónde va tu plata y decidir a dónde querés que vaya.",
            EducationCategory.Budgeting,
            3,
            null,
            """
            Un presupuesto no es una restricción: es un plan para tu plata.

            ## Paso 1: calculá tus ingresos netos
            Lo que efectivamente entra a tu cuenta cada mes.

            ## Paso 2: registrá tus gastos
            Durante un mes, anotá todo. FinGrow lo hace por vos si vinculás tus cuentas.

            ## Paso 3: aplicá la regla 50/30/20
            - **50%** necesidades
            - **30%** deseos
            - **20%** ahorro e inversión

            ## Paso 4: revisá cada mes
            Un presupuesto que no se revisa deja de servir en pocas semanas.
            """),
        new(
            "bonos-para-principiantes",
            "Bonos: qué son y cómo pagan",
            "Qué significa prestarle plata a un Estado o a una empresa y qué riesgos tiene.",
            EducationCategory.Investments,
            3,
            InvestmentType.Bond,
            """
            Un bono es un **préstamo** que le hacés a un Estado o a una empresa a cambio de intereses.

            ## Cómo pagan
            - **Cupones**: pagos periódicos de interés.
            - **Amortización**: la devolución del capital, al final o en cuotas.

            ## Riesgos a mirar
            - **Crédito**: que el emisor no pague.
            - **Tasa**: si suben las tasas, el precio del bono baja.
            - **Moneda**: en qué moneda paga y en cuál gastás vos.
            """),
        new(
            "acciones-para-principiantes",
            "Acciones: ser dueño de una parte de una empresa",
            "Cómo se gana y cómo se pierde con acciones, y por qué conviene diversificar.",
            EducationCategory.Investments,
            3,
            InvestmentType.Stock,
            """
            Comprar una acción es comprar una **parte de una empresa**.

            ## Cómo se gana
            - **Suba del precio** cuando la empresa crece.
            - **Dividendos**, si la empresa reparte ganancias.

            ## Cómo se pierde
            El precio puede bajar, y mucho, en el corto plazo. Por eso las acciones son para plata
            que no vas a necesitar en años.

            ## Diversificá
            Una sola empresa es un riesgo concentrado. Un ETF te da muchas empresas en una compra.
            """),
        new(
            "cripto-antes-de-invertir",
            "Criptomonedas: lo que tenés que saber antes de invertir",
            "Volatilidad, custodia y cuánto de tu portafolio tiene sentido destinar.",
            EducationCategory.Investments,
            3,
            InvestmentType.Crypto,
            """
            Las criptomonedas son activos digitales **muy volátiles**: caídas del 50% en meses no
            son raras.

            ## Antes de invertir
            - Invertí solo lo que podrías perder sin afectar tu vida.
            - Entendé **quién custodia** tus claves: un exchange o vos.
            - Desconfiá de cualquier promesa de rendimiento garantizado.

            ## Cuánto
            Para la mayoría de las personas, una porción chica del portafolio y solo después de
            tener un fondo de emergencia.
            """),
        new(
            "cedears-para-principiantes",
            "CEDEAR: acciones del exterior desde Argentina",
            "Cómo comprar Apple, Mercado Libre o el S&P 500 en pesos o en dólares sin abrir una cuenta afuera.",
            EducationCategory.Investments,
            3,
            InvestmentType.Cedear,
            """
            Un CEDEAR es un **certificado** que representa acciones o ETFs que cotizan en el exterior,
            y se compra y se vende en BYMA como cualquier acción local.

            ## Cómo se mueve su precio
            - Sigue al precio de la acción original **en dólares**.
            - Y al tipo de cambio implícito: si sube el dólar, sube el CEDEAR en pesos.

            ## El ratio
            Cada CEDEAR equivale a una fracción de la acción original. Por eso su precio no es el
            mismo que ves en Nueva York.

            ## Pesos o dólares
            El mismo CEDEAR tiene una variante en pesos (AAPL) y otra en dólares (AAPLD). Elegí la
            de la moneda con la que lo compraste.
            """),
        new(
            "obligaciones-negociables",
            "Obligaciones negociables: prestarle a una empresa",
            "Qué son las ON, por qué muchas pagan en dólares y qué mirar antes de comprar una.",
            EducationCategory.Investments,
            3,
            InvestmentType.CorporateBond,
            """
            Una obligación negociable (ON) es un **bono emitido por una empresa**: le prestás plata
            y te la devuelve con intereses.

            ## Cómo pagan
            - **Cupones** periódicos, muchas veces en dólares.
            - **Amortización** del capital al vencimiento o en cuotas.

            ## Qué mirar
            - **Quién emite**: la capacidad de pago de la empresa es tu principal riesgo.
            - **Lámina mínima**: la cantidad mínima de nominales que se puede comprar.
            - **Liquidez**: algunas ON se operan poco y cuesta venderlas antes de tiempo.

            Como los bonos, cotizan cada 100 nominales.
            """),
        new(
            "letras-del-tesoro",
            "Letras del Tesoro: LECAP y BONCAP",
            "Cómo funciona una tasa fija en pesos a corto plazo y en qué se diferencia de un plazo fijo.",
            EducationCategory.Investments,
            3,
            InvestmentType.TreasuryBill,
            """
            Las LECAP y los BONCAP son títulos del Tesoro en pesos con **tasa fija**: capitalizan
            intereses y pagan todo junto al vencimiento.

            ## Cómo se gana
            Comprás por debajo del valor que vas a cobrar al final. Esa diferencia, llevada a un
            mes, es la tasa efectiva mensual (TEM) que estás consiguiendo.

            ## Frente a un plazo fijo
            - Se pueden **vender antes** de que venzan.
            - Su precio cambia todos los días: si suben las tasas, baja.

            ## Riesgos
            Que la inflación termine siendo más alta que la tasa que fijaste. Cotizan cada 100
            nominales, con símbolos como S30N6.
            """),
        new(
            "plazo-fijo",
            "Plazo fijo: una tasa conocida desde el primer día",
            "TNA, TEA, plazo fijo UVA y qué pasa con tu plata si la inflación le gana a la tasa.",
            EducationCategory.Savings,
            3,
            InvestmentType.FixedTermDeposit,
            """
            En un plazo fijo depositás plata en un banco por un plazo y a una **tasa pactada**.
            Sabés desde el primer día cuánto vas a cobrar.

            ## TNA y TEA
            La TNA es la tasa anual sin capitalizar. Si renovás cada mes con los intereses, lo que
            ganás en el año se acerca a la TEA, que es mayor.

            ## Plazo fijo UVA
            Ajusta el capital por inflación y suma una tasa chica. Pide un plazo mínimo más largo.

            ## A tener en cuenta
            - No podés retirar la plata antes, salvo que sea precancelable.
            - Si la inflación supera la tasa, perdés poder de compra aunque el número crezca.
            """),
        new(
            "caucion-bursatil",
            "Caución: prestar a pocos días en la bolsa",
            "Una forma de hacer rendir pesos por días con la garantía del mercado.",
            EducationCategory.Investments,
            3,
            InvestmentType.Repo,
            """
            En una caución **colocadora** le prestás pesos a otro inversor por unos pocos días, a una
            tasa. El préstamo está garantizado por títulos y por el mercado.

            ## Para qué sirve
            Para plata que vas a usar pronto: se puede colocar a 1, 7 o 30 días y renovar.

            ## A tener en cuenta
            - La tasa cambia todos los días según la oferta y la demanda.
            - Las comisiones del broker y los derechos de mercado pesan mucho en plazos cortos:
              hacé la cuenta del rendimiento neto.
            """),
        new(
            "cuentas-remuneradas",
            "Cuentas remuneradas: rendimiento diario sin plazo",
            "Cómo rinden las cuentas de bancos y billeteras y por qué no todas tienen la misma protección.",
            EducationCategory.Savings,
            3,
            InvestmentType.RemuneratedAccount,
            """
            Muchas cuentas de bancos y billeteras virtuales **pagan intereses todos los días** sobre
            tu saldo, y la plata sigue disponible para usarla cuando quieras.

            ## Cómo rinden
            La tasa es variable y puede tener un tope de saldo. Lo que ganás hoy se suma al saldo y
            mañana también rinde.

            ## No son todas iguales
            En los bancos es una cuenta remunerada. En muchas billeteras, el saldo se invierte en
            un fondo money market: rinde parecido, pero no tiene la garantía de los depósitos.

            ## Para qué usarla
            Es un buen lugar para el fondo de emergencia, no para el ahorro de largo plazo.
            """),
    };
}

internal sealed record LessonSeed(string Title, int DurationMinutes, string VideoUrl);

internal sealed record CourseSeed(
    string Slug,
    string Title,
    string Description,
    CourseLevel Level,
    EducationCategory Category,
    InvestmentType? RelatedInvestmentType,
    IReadOnlyList<LessonSeed> Lessons);

internal sealed record ArticleSeed(
    string Slug,
    string Title,
    string Summary,
    EducationCategory Category,
    int ReadingTimeMinutes,
    InvestmentType? RelatedInvestmentType,
    string Content);
