namespace FinGrow.Infrastructure.Persistence.Seeding;

using FinGrow.Domain.Enums;

/// <summary>
/// El contenido educativo inicial de la plataforma (T-20). Los cursos y las lecciones son los
/// que ya mostraba el frontend como datos de prueba; los articulos se escribieron para esta
/// carga. El slug es la clave natural: cambiarlo hace que la carga lo trate como contenido nuevo.
/// </summary>
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
