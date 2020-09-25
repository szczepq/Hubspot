using System;
using AutoFixture;
using AutoFixture.Kernel;
using SoftwareHut.HubspotService.Test.ArgumentSpecimens;

namespace SoftwareHut.HubspotService.Test
{
    public class CustomizationsWithTargetValue : ICustomization, ISpecimenBuilder
    {
        public Type DeclaringType { get; }
        public string TargetName { get; }
        public object TargetValue { get; }

        public CustomizationsWithTargetValue(Type declaringType, string targetName, object targetValue)
        {
            DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            TargetName = targetName ?? throw new ArgumentNullException(nameof(targetName));
            TargetValue = targetValue;
        }

        public object Create(object request, ISpecimenContext context)
        {
            if (request == null)
                return new NoSpecimen();

            if (request.MatchesConstructorArgument(DeclaringType, TargetName,out var constructorArgumentType))
                return CreateSpecimen(constructorArgumentType);
            if (request.MatchesProperty(DeclaringType, TargetName, out var propertyType))
                return CreateSpecimen(propertyType);
            if (request.MatchesField(DeclaringType, TargetName, out var fieldType))
                return CreateSpecimen(fieldType);

            return new NoSpecimen();
        }

        public object CreateSpecimen(Type targetType)
        {
            // Allow passing decimal as string since attributes do not support decimal values
            if (targetType == typeof(decimal) &&
                TargetValue is string)
            {
                return decimal.Parse(TargetValue?.ToString());
            }

            // Allow passing nullable decimal as string since attributes do not support decimal values
            if (targetType == typeof(decimal?) &&
                TargetValue is string)
            {
                return decimal.TryParse(TargetValue?.ToString(), out var result) ? result : (decimal?)null;
            }

            // Allow passing URI as string since attributes do not support complex types
            if (targetType == typeof(Uri) && TargetValue is string)
            {
                return TargetValue == null ? null : new Uri(TargetValue.ToString());
            }

            return TargetValue;
        }

        public void Customize(IFixture fixture)
        {
            fixture.Customizations.Add(this);
        }
    }
}