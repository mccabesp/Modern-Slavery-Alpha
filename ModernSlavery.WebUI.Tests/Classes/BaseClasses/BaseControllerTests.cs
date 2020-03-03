using System;
using System.Linq;
using System.Reflection;
using ModernSlavery.WebUI.Controllers;
using ModernSlavery.WebUI.Models.Scope;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace ModernSlavery.WebUI.Tests.Classes.BaseClasses
{
    [TestFixture]
    [SetCulture("en-GB")]
    public class BaseControllerTests
    {
        [TestCase(typeof(RegisterController), "PINSent", null, typeof(AuthorizeAttribute))]
        [TestCase(
            typeof(ScopeController),
            "EnterOutOfScopeAnswers",
            typeof(EnterAnswersViewModel),
            typeof(ValidateAntiForgeryTokenAttribute))]
        public void BaseController_Verify_Some_Method_Is_Decorated_With_Some_Attribute(Type controllerType,
            string methodName,
            Type modelArgumentForTheMethod,
            Type customAttributeToLookFor)
        {
            MethodInfo methodInfo = modelArgumentForTheMethod != null
                ? controllerType.GetMethod(methodName, new[] {modelArgumentForTheMethod})
                : controllerType.GetMethod(methodName);

            Assert.NotNull(methodInfo, $"Expected '{controllerType.Name}' to contain method '{methodName}'");

            object[] attributes = methodInfo.GetCustomAttributes(customAttributeToLookFor, true);

            string methodArguments = modelArgumentForTheMethod != null
                ? $"{modelArgumentForTheMethod.Name} model"
                : string.Empty;

            Assert.IsTrue(
                attributes.Any(),
                $"Expected custom attribute '{customAttributeToLookFor.Name}' to be decorating method '{methodName}({methodArguments})'");
        }

    }
}
