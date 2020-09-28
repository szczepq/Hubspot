using AutoFixture;
using AutoFixture.Kernel;
using Microsoft.AspNetCore.Mvc;

namespace SoftwareHut.HubspotService.Test
{
    public class GreedyControllerConstructorAttribute : AutoFakeDataAttribute
    {
        public GreedyControllerConstructorAttribute()
            : base(new ConstructorCustomization(typeof(ControllerBase), new GreedyConstructorQuery()))
        {
        }
    }
}