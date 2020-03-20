using System;
using CommandLine;
using SGame;
using Xunit;
using System.Reflection;
using System.Diagnostics;
using SShared;

namespace SGame.Tests
{
    public class ApiAttrTester
    {
        [Fact]
        [ApiRoute("Route 17")]
        public void TestThatRouteIsCorrect()
        {
            TestAttrProperty<ApiRoute, string>(new StackFrame().GetMethod(),
                         "Route", "Route 17");
        }

        [Fact]
        [ApiParam("Apple", typeof(string))]
        public void TestThatParamIsCorrect()
        {
            TestAttrProperty<ApiParam, string>(new StackFrame().GetMethod(),
                         "Name", "Apple");
            TestAttrProperty<ApiParam, System.Type>(new StackFrame().GetMethod(),
                         "Type", typeof(string));
        }

        // Helpers Inspired by code from: https://www.codeproject.com/Articles/231959/Unit-Testing-Csharp-Custom-Attributes-with-NUnit-P
        private void TestAttrProperty<TAttr, TProp>(MethodBase method,
                     string argName, TProp expectedValue)
        {
            object[] customAttributes = method.GetCustomAttributes(typeof(TAttr), false);

            Assert.Single(customAttributes);

            TAttr attr = (TAttr)customAttributes[0];

            PropertyInfo propertyInfo = attr.GetType().GetProperty(argName);

            Assert.NotNull(propertyInfo);
            Assert.Equal(typeof(TProp), propertyInfo.PropertyType);
            Assert.True(propertyInfo.CanRead);
            Assert.True(propertyInfo.CanWrite);
            Assert.Equal(expectedValue, (TProp)propertyInfo.GetValue(attr, null));
        }
    }
}
