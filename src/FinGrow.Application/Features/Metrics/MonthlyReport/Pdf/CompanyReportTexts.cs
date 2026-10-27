namespace FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;

using FinGrow.Application.Features.Metrics.Trends;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;

/// <summary>
/// Los textos del PDF del reporte (HU-65) y del mail que lo lleva (HU-82), en los tres idiomas de la
/// plataforma. Los del PDF son los mismos que tenía el PDF que armaba el front (<c>companyReportPdf</c>,
/// <c>companyTrends</c> y <c>companyPanel</c> de <c>lib/i18n</c>): si cambian allá para el panel,
/// conviene cambiarlos acá también. Los números llegan ya formateados por <see cref="ReportLocale"/>.
/// </summary>
internal sealed class CompanyReportTexts
{
    public static readonly CompanyReportTexts Spanish = new()
    {
        Locale = ReportLocale.Spanish,
        FileName = "reporte-empresa",
        Title = "Reporte de Bienestar Financiero",
        Company = "Empresa",
        Period = "Período",
        ComparedWith = "Comparado con",
        GeneratedAt = "Generado el",
        SummaryTitle = "Resumen del mes",
        EvolutionTitle = "Evolución",
        TrendsTitle = "Tendencias",
        Anonymized = (month, minimum) =>
            $"En {month} hubo menos de {minimum} empleados activos. Los indicadores se ocultan para no identificar a nadie.",
        Participants = (participating, active) => $"{participating} de {active} empleados activos participaron",
        Band = label => $"Nivel: {label}",
        Bands = band => band switch
        {
            FinancialHealthBand.Critical => "Crítica",
            FinancialHealthBand.Fragile => "Frágil",
            FinancialHealthBand.Stable => "Estable",
            _ => "Saludable",
        },
        Hidden = "Oculto",
        HiddenTooFewActive = minimum => $"Menos de {minimum} empleados activos en el mes.",
        HiddenTooFewScored = minimum =>
            $"Menos de {minimum} empleados con actividad suficiente para medir su salud financiera.",
        Change = (change, month) => $"{change} respecto de {month}",
        NoChange = month => $"Sin cambios respecto de {month}",
        PreviousNoData = month => $"Sin datos de {month} para comparar.",
        PreviousTooFewActive = (month, minimum) => $"Sin comparación: en {month} hubo menos de {minimum} empleados activos.",
        PreviousTooFewScored = (month, minimum) => $"Sin comparación: en {month} hubo menos de {minimum} empleados con puntaje.",
        PrivacyNote = minimum =>
            "Este reporte contiene solo métricas agregadas y anónimas de la plantilla: ningún dato identifica a un empleado. "
            + $"Los indicadores de grupos de menos de {minimum} empleados no se muestran.",
        PositiveTrendsTitle = "Tendencias positivas",
        OpportunitiesTitle = "Oportunidades de mejora",
        NoPositiveTrends = "Ningún indicador mejoró de forma significativa.",
        NoOpportunities = "Ningún indicador empeoró de forma significativa.",
        NoTrendChanges = (current, previous) => $"Sin cambios significativos entre {previous} y {current}.",
        TrendsNoPeriod = "Todavía no hay meses cerrados para analizar.",
        TrendsNoPreviousPeriod = month =>
            $"Para ver tendencias hacen falta dos meses seguidos con datos. Por ahora solo está {month}.",
        TrendsAnonymized = minimum =>
            $"En alguno de los dos meses hubo menos de {minimum} empleados activos. Las tendencias se muestran cuando hay suficientes para no identificar a nadie.",
        TrendsFooter = "Se calculan a partir de indicadores agregados y anónimos. Un cambio menor a 2 puntos se considera estable.",
        FromTo = (previous, current) => $"De {previous} a {current}",
        Indicators = indicator => indicator switch
        {
            CompanyTrendIndicator.ParticipationRate => "Participación",
            CompanyTrendIndicator.AverageFinancialHealthScore => "Salud financiera promedio",
            CompanyTrendIndicator.BudgetAdoption => "Empleados con presupuesto",
            CompanyTrendIndicator.GoalAdoption => "Empleados con metas activas",
            CompanyTrendIndicator.IntegrationAdoption => "Empleados con integraciones",
            CompanyTrendIndicator.GoalsAchieved => "Metas alcanzadas",
            CompanyTrendIndicator.CompletedCourses => "Cursos completados",
            CompanyTrendIndicator.LowSavings => "Empleados con ahorro bajo",
            CompanyTrendIndicator.ThinCushion => "Empleados con fondo de emergencia insuficiente",
            CompanyTrendIndicator.BudgetOverruns => "Empleados que exceden su presupuesto",
            _ => "Empleados que no cubren la cuota de sus metas",
        },
        PointsChange = "puntos",
        PercentagePointsChange = "p.p.",
        ParticipationChartTitle = "Participación de Empleados",
        ParticipationChartDescription = "Usuarios activos vs empleados inscritos en el tiempo",
        ParticipationRateSeries = "Tasa de participación",
        ParticipatingEmployeesSeries = "Empleados participantes",
        HealthChartTitle = "Tendencia de Salud Financiera",
        HealthChartDescription = "Puntaje promedio de salud financiera de empleados",
        HealthScoreSeries = "Puntaje de Salud",
        NoPeriods = "No hay períodos para mostrar.",
        ScoreHiddenAll = "El puntaje se oculta en los meses con pocos empleados con actividad, para no identificar a nadie.",
        InsightsAnonymized = (month, minimum) =>
            $"En {month} menos de {minimum} empleados tuvieron actividad suficiente para medir su salud financiera. Se muestra cuando hay suficientes para no identificar a nadie.",
        NoClosedMonthsToAnalyze = "Todavía no hay meses cerrados para analizar.",
        ConcernsTitle = "Principales Preocupaciones Financieras",
        ConcernsDescription = "Desafíos financieros más comunes entre empleados",
        Measured = (month, count, isOne) => $"{month} · {count} {(isOne ? "empleado medido" : "empleados medidos")}",
        NoConcerns = (month, minimum) => $"En {month} no hubo preocupaciones que afecten a {minimum} empleados o más.",
        ConcernsFooter = minimum =>
            $"Solo se muestran las preocupaciones que afectan al menos a {minimum} empleados, para cuidar el anonimato.",
        AffectedOf = (affected, evaluated, label) => $"{affected} de {evaluated} {label}",
        Concerns = concern => concern switch
        {
            CompanyConcern.LowSavings => new ConcernTexts(
                "Ahorro bajo",
                "Ahorraron menos del 10 % de sus ingresos del mes.",
                isOne => isOne ? "empleado medido" : "empleados medidos"),
            CompanyConcern.ThinCushion => new ConcernTexts(
                "Fondo de emergencia insuficiente",
                "Lo que tienen acumulado cubre menos de un mes y medio de sus gastos.",
                isOne => isOne ? "empleado medido" : "empleados medidos"),
            CompanyConcern.BudgetOverruns => new ConcernTexts(
                "Presupuestos excedidos",
                "Se pasaron en más de la mitad de los topes de su presupuesto mensual.",
                isOne => isOne ? "empleado con presupuesto" : "empleados con presupuesto"),
            _ => new ConcernTexts(
                "Metas de ahorro sin cubrir",
                "Aportaron menos de la mitad de la cuota mensual de sus metas.",
                isOne => isOne ? "empleado con metas" : "empleados con metas"),
        },
        RecommendationsTitle = "Recomendaciones",
        RecommendationsDescription = "Acciones para mejorar el bienestar de empleados",
        RecommendationsUnavailable =
            "No pudimos generar recomendaciones ahora. Probá de nuevo más tarde; mientras tanto, los indicadores están en Principales Preocupaciones.",
        RecommendationsFooter = month =>
            $"Se generan automáticamente a partir de indicadores agregados y anónimos de {month}. Son orientativas y no constituyen asesoramiento financiero certificado.",
        RecommendationKinds = kind => kind switch
        {
            RecommendationKind.Positive => "Vas bien",
            RecommendationKind.Warning => "Atención",
            _ => "Sugerencia",
        },
        MailSubject = (company, month) => $"Reporte mensual de {company}: {month}",
        MailGreeting = company => $"Hola, {company}:",
        MailIntroduction = (month, previousMonth) =>
            $"Te adjuntamos el reporte de bienestar financiero de {month}, comparado con {previousMonth}: participación, salud financiera promedio, cursos completados, evolución, tendencias, preocupaciones y recomendaciones.",
        MailPanelLink = "Ver el panel de la empresa",
        MailFooter =
            "Recibís este correo porque activaste Reportes mensuales en Configuración → Notificaciones. Lo podés apagar desde ahí.",
    };

    public static readonly CompanyReportTexts English = new()
    {
        Locale = ReportLocale.English,
        FileName = "company-report",
        Title = "Financial Wellbeing Report",
        Company = "Company",
        Period = "Period",
        ComparedWith = "Compared with",
        GeneratedAt = "Generated on",
        SummaryTitle = "Month summary",
        EvolutionTitle = "Evolution over time",
        TrendsTitle = "Trends",
        Anonymized = (month, minimum) =>
            $"In {month} there were fewer than {minimum} active employees. Indicators are hidden to keep everyone anonymous.",
        Participants = (participating, active) => $"{participating} of {active} active employees took part",
        Band = label => $"Level: {label}",
        Bands = band => band switch
        {
            FinancialHealthBand.Critical => "Critical",
            FinancialHealthBand.Fragile => "Fragile",
            FinancialHealthBand.Stable => "Stable",
            _ => "Healthy",
        },
        Hidden = "Hidden",
        HiddenTooFewActive = minimum => $"Fewer than {minimum} active employees in the month.",
        HiddenTooFewScored = minimum =>
            $"Fewer than {minimum} employees with enough activity to measure their financial health.",
        Change = (change, month) => $"{change} compared with {month}",
        NoChange = month => $"No change compared with {month}",
        PreviousNoData = month => $"No data for {month} to compare with.",
        PreviousTooFewActive = (month, minimum) => $"No comparison: in {month} there were fewer than {minimum} active employees.",
        PreviousTooFewScored = (month, minimum) => $"No comparison: in {month} there were fewer than {minimum} employees with a score.",
        PrivacyNote = minimum =>
            "This report only contains aggregated, anonymous workforce metrics: no data identifies an employee. "
            + $"Indicators for groups of fewer than {minimum} employees are not shown.",
        PositiveTrendsTitle = "Positive trends",
        OpportunitiesTitle = "Opportunities for improvement",
        NoPositiveTrends = "No indicator improved significantly.",
        NoOpportunities = "No indicator got significantly worse.",
        NoTrendChanges = (current, previous) => $"No significant changes between {previous} and {current}.",
        TrendsNoPeriod = "There are no closed months to analyze yet.",
        TrendsNoPreviousPeriod = month => $"Trends need two consecutive months with data. For now there is only {month}.",
        TrendsAnonymized = minimum =>
            $"One of the two months had fewer than {minimum} active employees. Trends are shown when there are enough to keep everyone anonymous.",
        TrendsFooter = "Calculated from aggregated, anonymous indicators. A change smaller than 2 points is considered stable.",
        FromTo = (previous, current) => $"From {previous} to {current}",
        Indicators = indicator => indicator switch
        {
            CompanyTrendIndicator.ParticipationRate => "Participation",
            CompanyTrendIndicator.AverageFinancialHealthScore => "Average financial health",
            CompanyTrendIndicator.BudgetAdoption => "Employees with a budget",
            CompanyTrendIndicator.GoalAdoption => "Employees with active goals",
            CompanyTrendIndicator.IntegrationAdoption => "Employees with integrations",
            CompanyTrendIndicator.GoalsAchieved => "Goals achieved",
            CompanyTrendIndicator.CompletedCourses => "Courses completed",
            CompanyTrendIndicator.LowSavings => "Employees with low savings",
            CompanyTrendIndicator.ThinCushion => "Employees with an insufficient emergency fund",
            CompanyTrendIndicator.BudgetOverruns => "Employees who exceed their budget",
            _ => "Employees who miss their goal contributions",
        },
        PointsChange = "points",
        PercentagePointsChange = "pp",
        ParticipationChartTitle = "Employee Participation",
        ParticipationChartDescription = "Active users vs enrolled employees over time",
        ParticipationRateSeries = "Participation rate",
        ParticipatingEmployeesSeries = "Participating employees",
        HealthChartTitle = "Financial Health Trend",
        HealthChartDescription = "Average financial health score of employees",
        HealthScoreSeries = "Health Score",
        NoPeriods = "There are no periods to show.",
        ScoreHiddenAll = "The score is hidden in months with few active employees, so that no one can be identified.",
        InsightsAnonymized = (month, minimum) =>
            $"In {month} fewer than {minimum} employees had enough activity to measure their financial health. It's shown when there are enough to keep everyone anonymous.",
        NoClosedMonthsToAnalyze = "There are no closed months to analyze yet.",
        ConcernsTitle = "Top Financial Concerns",
        ConcernsDescription = "The most common financial challenges among employees",
        Measured = (month, count, isOne) => $"{month} · {count} {(isOne ? "employee measured" : "employees measured")}",
        NoConcerns = (month, minimum) => $"In {month} there were no concerns affecting {minimum} or more employees.",
        ConcernsFooter = minimum => $"Only concerns affecting at least {minimum} employees are shown, to protect anonymity.",
        AffectedOf = (affected, evaluated, label) => $"{affected} of {evaluated} {label}",
        Concerns = concern => concern switch
        {
            CompanyConcern.LowSavings => new ConcernTexts(
                "Low savings",
                "They saved less than 10% of their monthly income.",
                isOne => isOne ? "employee measured" : "employees measured"),
            CompanyConcern.ThinCushion => new ConcernTexts(
                "Insufficient emergency fund",
                "What they have saved covers less than a month and a half of their expenses.",
                isOne => isOne ? "employee measured" : "employees measured"),
            CompanyConcern.BudgetOverruns => new ConcernTexts(
                "Budgets exceeded",
                "They went over more than half of the limits in their monthly budget.",
                isOne => isOne ? "employee with a budget" : "employees with a budget"),
            _ => new ConcernTexts(
                "Savings goals not covered",
                "They contributed less than half of their goals' monthly installment.",
                isOne => isOne ? "employee with goals" : "employees with goals"),
        },
        RecommendationsTitle = "Recommendations",
        RecommendationsDescription = "Actions to improve employee wellbeing",
        RecommendationsUnavailable =
            "We couldn't generate recommendations right now. Try again later; in the meantime, the indicators are in Top Concerns.",
        RecommendationsFooter = month =>
            $"Generated automatically from aggregated, anonymous indicators for {month}. They are for guidance only and do not constitute certified financial advice.",
        RecommendationKinds = kind => kind switch
        {
            RecommendationKind.Positive => "On track",
            RecommendationKind.Warning => "Attention",
            _ => "Tip",
        },
        MailSubject = (company, month) => $"{company} monthly report: {month}",
        MailGreeting = company => $"Hello, {company}:",
        MailIntroduction = (month, previousMonth) =>
            $"Attached is the financial wellbeing report for {month}, compared with {previousMonth}: participation, average financial health, completed courses, evolution, trends, concerns and recommendations.",
        MailPanelLink = "Open the company dashboard",
        MailFooter =
            "You are receiving this email because you turned on Monthly reports in Settings → Notifications. You can turn it off there.",
    };

    public static readonly CompanyReportTexts Portuguese = new()
    {
        Locale = ReportLocale.Portuguese,
        FileName = "relatorio-empresa",
        Title = "Relatório de Bem-Estar Financeiro",
        Company = "Empresa",
        Period = "Período",
        ComparedWith = "Comparado com",
        GeneratedAt = "Gerado em",
        SummaryTitle = "Resumo do mês",
        EvolutionTitle = "Evolução",
        TrendsTitle = "Tendências",
        Anonymized = (month, minimum) =>
            $"Em {month} houve menos de {minimum} funcionários ativos. Os indicadores ficam ocultos para não identificar ninguém.",
        Participants = (participating, active) => $"{participating} de {active} funcionários ativos participaram",
        Band = label => $"Nível: {label}",
        Bands = band => band switch
        {
            FinancialHealthBand.Critical => "Crítica",
            FinancialHealthBand.Fragile => "Frágil",
            FinancialHealthBand.Stable => "Estável",
            _ => "Saudável",
        },
        Hidden = "Oculto",
        HiddenTooFewActive = minimum => $"Menos de {minimum} funcionários ativos no mês.",
        HiddenTooFewScored = minimum =>
            $"Menos de {minimum} funcionários com atividade suficiente para medir a saúde financeira.",
        Change = (change, month) => $"{change} em relação a {month}",
        NoChange = month => $"Sem mudanças em relação a {month}",
        PreviousNoData = month => $"Sem dados de {month} para comparar.",
        PreviousTooFewActive = (month, minimum) => $"Sem comparação: em {month} houve menos de {minimum} funcionários ativos.",
        PreviousTooFewScored = (month, minimum) => $"Sem comparação: em {month} houve menos de {minimum} funcionários com pontuação.",
        PrivacyNote = minimum =>
            "Este relatório contém apenas métricas agregadas e anônimas da equipe: nenhum dado identifica um funcionário. "
            + $"Os indicadores de grupos com menos de {minimum} funcionários não são exibidos.",
        PositiveTrendsTitle = "Tendências positivas",
        OpportunitiesTitle = "Oportunidades de melhoria",
        NoPositiveTrends = "Nenhum indicador melhorou de forma significativa.",
        NoOpportunities = "Nenhum indicador piorou de forma significativa.",
        NoTrendChanges = (current, previous) => $"Sem mudanças significativas entre {previous} e {current}.",
        TrendsNoPeriod = "Ainda não há meses fechados para analisar.",
        TrendsNoPreviousPeriod = month =>
            $"Para ver tendências são necessários dois meses seguidos com dados. Por enquanto só há {month}.",
        TrendsAnonymized = minimum =>
            $"Em algum dos dois meses houve menos de {minimum} funcionários ativos. As tendências são exibidas quando há o suficiente para não identificar ninguém.",
        TrendsFooter = "Calculadas a partir de indicadores agregados e anônimos. Uma mudança menor que 2 pontos é considerada estável.",
        FromTo = (previous, current) => $"De {previous} para {current}",
        Indicators = indicator => indicator switch
        {
            CompanyTrendIndicator.ParticipationRate => "Participação",
            CompanyTrendIndicator.AverageFinancialHealthScore => "Saúde financeira média",
            CompanyTrendIndicator.BudgetAdoption => "Funcionários com orçamento",
            CompanyTrendIndicator.GoalAdoption => "Funcionários com metas ativas",
            CompanyTrendIndicator.IntegrationAdoption => "Funcionários com integrações",
            CompanyTrendIndicator.GoalsAchieved => "Metas alcançadas",
            CompanyTrendIndicator.CompletedCourses => "Cursos concluídos",
            CompanyTrendIndicator.LowSavings => "Funcionários com poupança baixa",
            CompanyTrendIndicator.ThinCushion => "Funcionários com reserva de emergência insuficiente",
            CompanyTrendIndicator.BudgetOverruns => "Funcionários que excedem o orçamento",
            _ => "Funcionários que não cobrem a parcela das metas",
        },
        PointsChange = "pontos",
        PercentagePointsChange = "p.p.",
        ParticipationChartTitle = "Participação dos Funcionários",
        ParticipationChartDescription = "Usuários ativos vs funcionários inscritos ao longo do tempo",
        ParticipationRateSeries = "Taxa de participação",
        ParticipatingEmployeesSeries = "Funcionários participantes",
        HealthChartTitle = "Tendência de Saúde Financeira",
        HealthChartDescription = "Pontuação média de saúde financeira dos funcionários",
        HealthScoreSeries = "Pontuação de Saúde",
        NoPeriods = "Não há períodos para mostrar.",
        ScoreHiddenAll = "A pontuação fica oculta nos meses com poucos funcionários ativos, para não identificar ninguém.",
        InsightsAnonymized = (month, minimum) =>
            $"Em {month} menos de {minimum} funcionários tiveram atividade suficiente para medir sua saúde financeira. É exibida quando há funcionários suficientes para não identificar ninguém.",
        NoClosedMonthsToAnalyze = "Ainda não há meses fechados para analisar.",
        ConcernsTitle = "Principais Preocupações Financeiras",
        ConcernsDescription = "Desafios financeiros mais comuns entre os funcionários",
        Measured = (month, count, isOne) => $"{month} · {count} {(isOne ? "funcionário medido" : "funcionários medidos")}",
        NoConcerns = (month, minimum) => $"Em {month} não houve preocupações que afetassem {minimum} funcionários ou mais.",
        ConcernsFooter = minimum =>
            $"Só são exibidas as preocupações que afetam pelo menos {minimum} funcionários, para preservar o anonimato.",
        AffectedOf = (affected, evaluated, label) => $"{affected} de {evaluated} {label}",
        Concerns = concern => concern switch
        {
            CompanyConcern.LowSavings => new ConcernTexts(
                "Poupança baixa",
                "Pouparam menos de 10% da renda do mês.",
                isOne => isOne ? "funcionário medido" : "funcionários medidos"),
            CompanyConcern.ThinCushion => new ConcernTexts(
                "Reserva de emergência insuficiente",
                "O que têm acumulado cobre menos de um mês e meio de despesas.",
                isOne => isOne ? "funcionário medido" : "funcionários medidos"),
            CompanyConcern.BudgetOverruns => new ConcernTexts(
                "Orçamentos excedidos",
                "Ultrapassaram mais da metade dos limites do orçamento mensal.",
                isOne => isOne ? "funcionário com orçamento" : "funcionários com orçamento"),
            _ => new ConcernTexts(
                "Metas de poupança sem cobertura",
                "Aportaram menos da metade da parcela mensal de suas metas.",
                isOne => isOne ? "funcionário com metas" : "funcionários com metas"),
        },
        RecommendationsTitle = "Recomendações",
        RecommendationsDescription = "Ações para melhorar o bem-estar dos funcionários",
        RecommendationsUnavailable =
            "Não conseguimos gerar recomendações agora. Tente novamente mais tarde; enquanto isso, os indicadores estão em Principais Preocupações.",
        RecommendationsFooter = month =>
            $"São geradas automaticamente a partir de indicadores agregados e anônimos de {month}. São apenas orientativas e não constituem consultoria financeira certificada.",
        RecommendationKinds = kind => kind switch
        {
            RecommendationKind.Positive => "Indo bem",
            RecommendationKind.Warning => "Atenção",
            _ => "Sugestão",
        },
        MailSubject = (company, month) => $"Relatório mensal de {company}: {month}",
        MailGreeting = company => $"Olá, {company}:",
        MailIntroduction = (month, previousMonth) =>
            $"Segue em anexo o relatório de bem-estar financeiro de {month}, comparado com {previousMonth}: participação, saúde financeira média, cursos concluídos, evolução, tendências, preocupações e recomendações.",
        MailPanelLink = "Ver o painel da empresa",
        MailFooter =
            "Você recebe este e-mail porque ativou Relatórios mensais em Configurações → Notificações. Você pode desativá-lo por lá.",
    };

    public required ReportLocale Locale { get; init; }

    public required string FileName { get; init; }

    public required string Title { get; init; }

    public required string Company { get; init; }

    public required string Period { get; init; }

    public required string ComparedWith { get; init; }

    public required string GeneratedAt { get; init; }

    public required string SummaryTitle { get; init; }

    public required string EvolutionTitle { get; init; }

    public required string TrendsTitle { get; init; }

    /// <summary>(mes, mínimo)</summary>
    public required Func<string, string, string> Anonymized { get; init; }

    /// <summary>(participantes, activos)</summary>
    public required Func<string, string, string> Participants { get; init; }

    public required Func<string, string> Band { get; init; }

    public required Func<FinancialHealthBand, string> Bands { get; init; }

    public required string Hidden { get; init; }

    public required Func<string, string> HiddenTooFewActive { get; init; }

    public required Func<string, string> HiddenTooFewScored { get; init; }

    /// <summary>(variación, mes anterior)</summary>
    public required Func<string, string, string> Change { get; init; }

    public required Func<string, string> NoChange { get; init; }

    public required Func<string, string> PreviousNoData { get; init; }

    /// <summary>(mes anterior, mínimo)</summary>
    public required Func<string, string, string> PreviousTooFewActive { get; init; }

    /// <summary>(mes anterior, mínimo)</summary>
    public required Func<string, string, string> PreviousTooFewScored { get; init; }

    public required Func<string, string> PrivacyNote { get; init; }

    public required string PositiveTrendsTitle { get; init; }

    public required string OpportunitiesTitle { get; init; }

    public required string NoPositiveTrends { get; init; }

    public required string NoOpportunities { get; init; }

    /// <summary>(mes actual, mes anterior)</summary>
    public required Func<string, string, string> NoTrendChanges { get; init; }

    public required string TrendsNoPeriod { get; init; }

    public required Func<string, string> TrendsNoPreviousPeriod { get; init; }

    public required Func<string, string> TrendsAnonymized { get; init; }

    public required string TrendsFooter { get; init; }

    /// <summary>(valor anterior, valor actual)</summary>
    public required Func<string, string, string> FromTo { get; init; }

    public required Func<CompanyTrendIndicator, string> Indicators { get; init; }

    public required string PointsChange { get; init; }

    public required string PercentagePointsChange { get; init; }

    public required string ParticipationChartTitle { get; init; }

    public required string ParticipationChartDescription { get; init; }

    public required string ParticipationRateSeries { get; init; }

    public required string ParticipatingEmployeesSeries { get; init; }

    public required string HealthChartTitle { get; init; }

    public required string HealthChartDescription { get; init; }

    public required string HealthScoreSeries { get; init; }

    public required string NoPeriods { get; init; }

    public required string ScoreHiddenAll { get; init; }

    /// <summary>(mes, mínimo)</summary>
    public required Func<string, string, string> InsightsAnonymized { get; init; }

    public required string NoClosedMonthsToAnalyze { get; init; }

    public required string ConcernsTitle { get; init; }

    public required string ConcernsDescription { get; init; }

    /// <summary>(mes, cantidad, si es uno solo)</summary>
    public required Func<string, string, bool, string> Measured { get; init; }

    /// <summary>(mes, mínimo)</summary>
    public required Func<string, string, string> NoConcerns { get; init; }

    public required Func<string, string> ConcernsFooter { get; init; }

    /// <summary>(afectados, evaluados, etiqueta)</summary>
    public required Func<string, string, string, string> AffectedOf { get; init; }

    public required Func<CompanyConcern, ConcernTexts> Concerns { get; init; }

    public required string RecommendationsTitle { get; init; }

    public required string RecommendationsDescription { get; init; }

    public required string RecommendationsUnavailable { get; init; }

    public required Func<string, string> RecommendationsFooter { get; init; }

    public required Func<RecommendationKind, string> RecommendationKinds { get; init; }

    /// <summary>(empresa, mes)</summary>
    public required Func<string, string, string> MailSubject { get; init; }

    public required Func<string, string> MailGreeting { get; init; }

    /// <summary>(mes, mes anterior)</summary>
    public required Func<string, string, string> MailIntroduction { get; init; }

    public required string MailPanelLink { get; init; }

    public required string MailFooter { get; init; }

    public static CompanyReportTexts For(Language language) => language switch
    {
        Language.en => English,
        Language.pt => Portuguese,
        _ => Spanish,
    };

    /// <summary>Un valor con su unidad, como <c>companyTrends.values</c>: "80%", "70/100", "6".</summary>
    public string Value(CompanyTrendUnit unit, decimal value) => unit switch
    {
        CompanyTrendUnit.Percentage => Locale.Number(value, 1) + "%",
        CompanyTrendUnit.Points => Locale.Number(value, 2) + "/100",
        _ => Locale.Number(value),
    };

    /// <summary>Una variación con signo y unidad, como <c>companyTrends.changes</c>: "+30 p.p.", "-2 puntos", "+4".</summary>
    public string ChangeOf(CompanyTrendUnit unit, decimal change)
    {
        var digits = unit switch
        {
            CompanyTrendUnit.Percentage => 1,
            CompanyTrendUnit.Points => 2,
            _ => 0,
        };
        var signed = (change > 0 ? "+" : "-") + Locale.Number(Math.Abs(change), digits);

        return unit switch
        {
            CompanyTrendUnit.Percentage => signed + " " + PercentagePointsChange,
            CompanyTrendUnit.Points => signed + " " + PointsChange,
            _ => signed,
        };
    }
}

/// <summary>Título, descripción y cómo se llama a los evaluados de una preocupación (HU-61).</summary>
internal sealed record ConcernTexts(string Title, string Description, Func<bool, string> Evaluated);
