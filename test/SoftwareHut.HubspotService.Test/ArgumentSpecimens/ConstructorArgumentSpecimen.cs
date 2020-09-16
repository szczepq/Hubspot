using System;
using AutoFixture.Kernel;

namespace SoftwareHut.HubspotService.Test.ArgumentSpecimens
{
    public class ConstructorArgumentSpecimen<TTarget, TParameterType> : ISpecimenBuilder
    {
        private readonly string _paramName;
        private readonly Func<TParameterType> _valueFactory;

        public ConstructorArgumentSpecimen(string paramName, Func<TParameterType> valueFactory)
        {
            _paramName = paramName;
            _valueFactory = valueFactory;
        }

        public object Create(object request, ISpecimenContext context) =>
            request.IsParameterDeclaredInCtor<TTarget, TParameterType>(_paramName)
                ? (object) _valueFactory.Invoke()
                : new NoSpecimen();
    }
}