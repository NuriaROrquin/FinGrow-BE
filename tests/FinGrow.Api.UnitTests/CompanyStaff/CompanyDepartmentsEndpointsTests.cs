namespace FinGrow.Api.UnitTests.CompanyStaff;

using System.Net;
using System.Net.Http.Json;
using FinGrow.Application.Common;

public class CompanyDepartmentsEndpointsTests
{
    private const string DepartmentsPath = "/api/company/departments";

    [Fact]
    public async Task Creating_a_department_persists_it_in_the_company_of_the_token()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateCompanyClient();

        var response = await client.PostAsJsonAsync(
            new Uri(DepartmentsPath, UriKind.Relative),
            new { name = "Ventas", description = "Ventas y atención al cliente", managerName = "María González" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var department = factory.Company.Departments.ShouldHaveSingleItem();
        department.Name.ShouldBe("Ventas");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain($"\"id\":\"{department.Id}\"");
        body.ShouldContain("\"employeeCount\":0");
    }

    [Fact]
    public async Task A_department_without_a_name_responds_400()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateCompanyClient();

        var response = await client.PostAsJsonAsync(
            new Uri(DepartmentsPath, UriKind.Relative),
            new { name = "  ", description = "Sin nombre" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Company.Departments.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_repeated_name_responds_409()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        factory.Company.AddDepartment("Ventas", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        var client = factory.CreateCompanyClient();

        var response = await client.PostAsJsonAsync(new Uri(DepartmentsPath, UriKind.Relative), new { name = "ventas" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_list_includes_how_many_employees_each_department_has()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var sales = factory.Company.AddDepartment("Ventas", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        factory.AddEmployee(sales);
        var client = factory.CreateCompanyClient();

        var response = await client.GetAsync(new Uri(DepartmentsPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"name\":\"Ventas\"");
        body.ShouldContain("\"employeeCount\":1");
    }

    [Fact]
    public async Task Editing_a_department_saves_the_new_details()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var sales = factory.Company.AddDepartment("Ventas", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        var client = factory.CreateCompanyClient();

        var response = await client.PutAsJsonAsync(
            new Uri($"{DepartmentsPath}/{sales.Id}", UriKind.Relative),
            new { name = "Ventas Online", description = "Canal digital", managerName = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        sales.Name.ShouldBe("Ventas Online");
        sales.Description.ShouldBe("Canal digital");
    }

    [Fact]
    public async Task Deleting_an_empty_department_responds_204()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var sales = factory.Company.AddDepartment("Ventas", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        var client = factory.CreateCompanyClient();

        var response = await client.DeleteAsync(new Uri($"{DepartmentsPath}/{sales.Id}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        factory.Company.Departments.ShouldBeEmpty();
    }

    [Fact]
    public async Task Deleting_a_department_with_employees_responds_409_with_how_many_depend_on_it()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var sales = factory.Company.AddDepartment("Ventas", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        factory.AddEmployee(sales, "ana@acme.com");
        factory.AddEmployee(sales, "bruno@acme.com");
        var client = factory.CreateCompanyClient();

        var response = await client.DeleteAsync(new Uri($"{DepartmentsPath}/{sales.Id}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).ShouldContain("tiene 2 empleados asignados");
        factory.Company.Departments.ShouldContain(sales);
    }

    [Fact]
    public async Task Deactivating_a_department_moves_its_employees_and_hides_it_from_assignments()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var marketing = factory.Company.AddDepartment("Marketing", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        var sales = factory.Company.AddDepartment("Ventas", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        var ana = factory.AddEmployee(marketing, "ana@acme.com");
        var client = factory.CreateCompanyClient();

        var response = await client.PutAsJsonAsync(
            new Uri($"{DepartmentsPath}/{marketing.Id}/status", UriKind.Relative),
            new { isActive = false, moveEmployeesTo = sales.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        marketing.IsActive.ShouldBeFalse();
        ana.DepartmentId.ShouldBe(sales.Id);
        (await response.Content.ReadAsStringAsync()).ShouldContain("\"isActive\":false");

        var assignment = await client.PostAsJsonAsync(
            new Uri("/api/company/employees", UriKind.Relative),
            new { fullName = "Bruno Díaz", email = "bruno@acme.com", departmentId = marketing.Id });
        assignment.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reactivating_a_department_makes_it_available_for_assignments_again()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var marketing = factory.Company.AddDepartment("Marketing", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        marketing.Deactivate(CompanyStaffWebApplicationFactory.CreatedAt);
        var client = factory.CreateCompanyClient();

        var response = await client.PutAsJsonAsync(
            new Uri($"{DepartmentsPath}/{marketing.Id}/status", UriKind.Relative),
            new { isActive = true });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        marketing.IsActive.ShouldBeTrue();

        var assignment = await client.PostAsJsonAsync(
            new Uri("/api/company/employees", UriKind.Relative),
            new { fullName = "Bruno Díaz", email = "bruno@acme.com", departmentId = marketing.Id });
        assignment.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Changing_the_status_without_saying_which_responds_400()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var marketing = factory.Company.AddDepartment("Marketing", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        var client = factory.CreateCompanyClient();

        var response = await client.PutAsJsonAsync(
            new Uri($"{DepartmentsPath}/{marketing.Id}/status", UriKind.Relative),
            new { moveEmployeesTo = (Guid?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        marketing.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task An_employee_token_is_rejected_with_403()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateClientFor(Rol.Empleado);

        var response = await client.GetAsync(new Uri(DepartmentsPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
