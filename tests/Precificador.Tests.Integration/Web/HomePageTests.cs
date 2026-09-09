namespace Precificador.Tests.Integration.Web;

public sealed class HomePageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Get_root_returns_success_status_code()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
    }
}
