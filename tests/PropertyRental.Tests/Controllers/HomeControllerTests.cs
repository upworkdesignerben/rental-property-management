using Microsoft.AspNetCore.Mvc;
using PropertyRental.Web.Controllers;

namespace PropertyRental.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void IndexReturnsView()
    {
        var controller = new HomeController();

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}
