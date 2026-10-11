namespace FinGrow.Api.UnitTests.CompanyStaff;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Domain.ValueObjects;

public class CompanyEmployeesEndpointsTests
{
    private const string EmployeesPath = "/api/company/employees";

    [Fact]
    public async Task Creating_an_employee_returns_it_with_a_temporary_password_that_logs_in()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var sales = factory.Company.AddDepartment("Ventas", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        var client = factory.CreateCompanyClient();

        var response = await client.PostAsJsonAsync(
            new Uri(EmployeesPath, UriKind.Relative),
            new { fullName = "Juan Pérez", email = "juan.perez@acme.com", phoneNumber = (string?)null, departmentId = sales.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var employee = factory.Employees.ShouldHaveSingleItem();
        employee.CompanyId.ShouldBe(factory.Company.Id);
        employee.DepartmentId.ShouldBe(sales.Id);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var temporaryPassword = body.RootElement.GetProperty("temporaryPassword").GetString();
        temporaryPassword.ShouldNotBeNullOrWhiteSpace();
        body.RootElement.GetProperty("employee").GetProperty("departmentName").GetString().ShouldBe("Ventas");

        var login = await factory.CreateClient().PostAsJsonAsync(
            new Uri("/login/empleado", UriKind.Relative),
            new { email = "juan.perez@acme.com", password = temporaryPassword });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_email_that_already_exists_responds_409()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        factory.AddEmployee(department: null, email: "ana@acme.com");
        var client = factory.CreateCompanyClient();

        var response = await client.PostAsJsonAsync(
            new Uri(EmployeesPath, UriKind.Relative),
            new { fullName = "Ana Gómez", email = "Ana@Acme.com" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        factory.Employees.Count.ShouldBe(1);
    }

    [Fact]
    public async Task An_invalid_email_responds_400()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateCompanyClient();

        var response = await client.PostAsJsonAsync(
            new Uri(EmployeesPath, UriKind.Relative),
            new { fullName = "Ana Gómez", email = "ana-sin-arroba" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Employees.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_list_shows_the_employees_of_the_company()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var sales = factory.Company.AddDepartment("Ventas", null, null, CompanyStaffWebApplicationFactory.CreatedAt);
        factory.AddEmployee(sales, "ana@acme.com");
        var client = factory.CreateCompanyClient();

        var response = await client.GetAsync(new Uri(EmployeesPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"email\":\"ana@acme.com\"");
        body.ShouldContain("\"departmentName\":\"Ventas\"");
        body.ShouldNotContain("password", Case.Insensitive);
    }

    [Fact]
    public async Task Editing_an_employee_saves_the_new_details()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var employee = factory.AddEmployee(department: null, email: "ana@acme.com");
        var client = factory.CreateCompanyClient();

        var response = await client.PutAsJsonAsync(
            new Uri($"{EmployeesPath}/{employee.Id}", UriKind.Relative),
            new { fullName = "Ana María Gómez", email = "amgomez@acme.com", phoneNumber = "+54 11 4000-0000" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        employee.FullName.ShouldBe("Ana María Gómez");
        employee.Email.Value.ShouldBe("amgomez@acme.com");
    }

    [Fact]
    public async Task Deleting_an_employee_removes_it_and_clears_their_notifications()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var employee = factory.AddEmployee(department: null, email: "ana@acme.com");
        var client = factory.CreateCompanyClient();

        var response = await client.DeleteAsync(new Uri($"{EmployeesPath}/{employee.Id}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        factory.Employees.ShouldBeEmpty();
        factory.ClearedRecipients.ShouldHaveSingleItem().ShouldBe(NotificationRecipient.Employee(employee.Id));
    }

    [Fact]
    public async Task Deleting_an_unknown_employee_responds_404()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateCompanyClient();

        var response = await client.DeleteAsync(new Uri($"{EmployeesPath}/{Guid.CreateVersion7()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_employee_token_is_rejected_with_403()
    {
        using var factory = new CompanyStaffWebApplicationFactory();
        var client = factory.CreateClientFor(Rol.Empleado);

        var response = await client.GetAsync(new Uri(EmployeesPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
