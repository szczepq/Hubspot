using Microsoft.AspNetCore.Mvc;
using SoftwareHut.HubspotService.Repositories;
using System;
using System.Threading.Tasks;

namespace SoftwareHut.HubspotService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public IUserRepository UserRepository { get; }
        public TestController(IUserRepository userRepository)
        {
            UserRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task Test()
        {
            await UserRepository.CreateUserAsync(1, "ss");
        }
    }
}
