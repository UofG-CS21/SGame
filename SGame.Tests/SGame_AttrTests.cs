using System;
using CommandLine;
using SGame;
using Xunit;
using System.Reflection;
using System.Diagnostics;


namespace SGame.Tests
{
    public class ApiAttrTester
    {
        [Fact]
        [ApiRoute("Route 17")]
        public void TestThatRouteIsRoute17()
        {
            TestAttrProperty<ApiRoute, string>(new StackFrame().GetMethod(),
                         "Route", "Route 17");
        }

        // Helpers
        private void TestAttrProperty<TAttr, TProp>(MethodBase method,
                     string argName, TProp expectedValue)
        {
            object[] customAttributes = method.GetCustomAttributes(typeof(TAttr), false);

            Assert.Equal(1, customAttributes.Length);

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