using System;
using AutoFixture.Kernel;

namespace SoftwareHut.HubspotService.Test.ArgumentSpecimens
{
    public class FieldArgumentSpecimen<TTarget, TParameterType> : ISpecimenBuilder
    {
        private readonly string _paramName;
        private readonly Func<TParameterType> _valueFactory;

        public FieldArgumentSpecimen(string paramName, Func<TParameterType> valueFactory)
        {
            _paramName = paramName;
            _valueFactory = valueFactory;
        }

        public object Create(object request, ISpecimenContext context) =>
            request.IsFieldDeclaredIn<TTarget, TParameterType>(_paramName)
                ? (object)_valueFactory.Invoke()
                : new NoSpecimen();
    }
}