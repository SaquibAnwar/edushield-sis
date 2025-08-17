using EduShield.Api.Auth.Handlers;
using EduShield.Api.Auth.Requirements;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace EduShield.Api.Tests;

[TestFixture]
public class AuthorizationHandlerTests
{
    [TestFixture]
    public class StudentResourceAuthorizationHandlerTests
    {
        private StudentResourceAuthorizationHandler _handler;

        [SetUp]
        public void Setup()
        {
            _handler = new StudentResourceAuthorizationHandler();
        }

        [Test]
        public async Task HandleRequirementAsync_SystemAdmin_Succeeds()
        {
            // Arrange
            var user = CreateUser(UserRole.SystemAdmin);
            var student = new Student { StudentId = Guid.NewGuid() };
            var context = CreateAuthorizationContext(user, new StudentAccessRequirement(), student);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.True);
        }

        [Test]
        public async Task HandleRequirementAsync_SchoolAdmin_Succeeds()
        {
            // Arrange
            var user = CreateUser(UserRole.SchoolAdmin);
            var student = new Student { StudentId = Guid.NewGuid() };
            var context = CreateAuthorizationContext(user, new StudentAccessRequirement(), student);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.True);
        }

        [Test]
        public async Task HandleRequirementAsync_Teacher_Succeeds()
        {
            // Arrange
            var user = CreateUser(UserRole.Teacher);
            var student = new Student { StudentId = Guid.NewGuid() };
            var context = CreateAuthorizationContext(user, new StudentAccessRequirement(), student);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.True);
        }

        [Test]
        public async Task HandleRequirementAsync_StudentAccessingOwnRecord_Succeeds()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var user = CreateUser(UserRole.Student, studentId);
            var student = new Student { StudentId = studentId };
            var context = CreateAuthorizationContext(user, new StudentAccessRequirement(), student);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.True);
        }

        [Test]
        public async Task HandleRequirementAsync_StudentAccessingOtherRecord_Fails()
        {
            // Arrange
            var user = CreateUser(UserRole.Student, Guid.NewGuid());
            var student = new Student { StudentId = Guid.NewGuid() }; // Different student
            var context = CreateAuthorizationContext(user, new StudentAccessRequirement(), student);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.That(context.HasSucceeded, Is.False);
        }

        private static ClaimsPrincipal CreateUser(UserRole role, Guid? studentId = null, Guid? parentId = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new(ClaimTypes.Role, role.ToString())
            };

            if (studentId.HasValue)
                claims.Add(new Claim("StudentId", studentId.Value.ToString()));

            if (parentId.HasValue)
                claims.Add(new Claim("ParentId", parentId.Value.ToString()));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        private static AuthorizationHandlerContext CreateAuthorizationContext(
            ClaimsPrincipal user,
            IAuthorizationRequirement requirement,
            object resource)
        {
            return new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource);
        }
    }
}