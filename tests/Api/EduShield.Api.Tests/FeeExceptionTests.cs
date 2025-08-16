using NUnit.Framework;
using EduShield.Core.Exceptions;

namespace EduShield.Api.Tests;

public class FeeExceptionTests
{
    [Test]
    public void FeeNotFoundException_WithFeeId_SetsPropertiesCorrectly()
    {
        // Arrange
        var feeId = Guid.NewGuid();

        // Act
        var exception = new FeeNotFoundException(feeId);

        // Assert
        Assert.That(exception.FeeId, Is.EqualTo(feeId));
        Assert.That(exception.Message, Does.Contain(feeId.ToString()));
        Assert.That(exception.Message, Does.Contain("Fee with ID"));
        Assert.That(exception.Message, Does.Contain("was not found"));
    }

    [Test]
    public void FeeNotFoundException_WithFeeIdAndMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var customMessage = "Custom error message";

        // Act
        var exception = new FeeNotFoundException(feeId, customMessage);

        // Assert
        Assert.That(exception.FeeId, Is.EqualTo(feeId));
        Assert.That(exception.Message, Is.EqualTo(customMessage));
    }

    [Test]
    public void StudentNotFoundException_WithStudentId_SetsPropertiesCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();

        // Act
        var exception = new StudentNotFoundException(studentId);

        // Assert
        Assert.That(exception.StudentId, Is.EqualTo(studentId));
        Assert.That(exception.Message, Does.Contain(studentId.ToString()));
        Assert.That(exception.Message, Does.Contain("Student with ID"));
        Assert.That(exception.Message, Does.Contain("was not found"));
    }

    [Test]
    public void StudentNotFoundException_WithStudentIdAndMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var customMessage = "Custom error message";

        // Act
        var exception = new StudentNotFoundException(studentId, customMessage);

        // Assert
        Assert.That(exception.StudentId, Is.EqualTo(studentId));
        Assert.That(exception.Message, Is.EqualTo(customMessage));
    }

    [Test]
    public void InvalidPaymentAmountException_WithAmounts_SetsPropertiesCorrectly()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var paymentAmount = 1500.00m;
        var outstandingAmount = 1000.00m;

        // Act
        var exception = new InvalidPaymentAmountException(feeId, paymentAmount, outstandingAmount);

        // Assert
        Assert.That(exception.FeeId, Is.EqualTo(feeId));
        Assert.That(exception.PaymentAmount, Is.EqualTo(paymentAmount));
        Assert.That(exception.OutstandingAmount, Is.EqualTo(outstandingAmount));
        Assert.That(exception.Message, Does.Contain(paymentAmount.ToString("C")));
        Assert.That(exception.Message, Does.Contain(outstandingAmount.ToString("C")));
        Assert.That(exception.Message, Does.Contain("exceeds outstanding balance"));
    }

    [Test]
    public void PaymentNotFoundException_WithPaymentId_SetsPropertiesCorrectly()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        // Act
        var exception = new PaymentNotFoundException(paymentId);

        // Assert
        Assert.That(exception.PaymentId, Is.EqualTo(paymentId));
        Assert.That(exception.Message, Does.Contain(paymentId.ToString()));
        Assert.That(exception.Message, Does.Contain("Payment with ID"));
        Assert.That(exception.Message, Does.Contain("was not found"));
    }

    [Test]
    public void FeeBusinessRuleException_WithBusinessRule_SetsPropertiesCorrectly()
    {
        // Arrange
        var businessRule = "FeeCannotBeModifiedAfterPayment";
        var message = "Fee cannot be modified after payment has been made";

        // Act
        var exception = new FeeBusinessRuleException(businessRule, message);

        // Assert
        Assert.That(exception.BusinessRule, Is.EqualTo(businessRule));
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.FeeId, Is.Null);
    }

    [Test]
    public void FeeBusinessRuleException_WithBusinessRuleAndFeeId_SetsPropertiesCorrectly()
    {
        // Arrange
        var businessRule = "FeeCannotBeModifiedAfterPayment";
        var message = "Fee cannot be modified after payment has been made";
        var feeId = Guid.NewGuid();

        // Act
        var exception = new FeeBusinessRuleException(businessRule, message, feeId);

        // Assert
        Assert.That(exception.BusinessRule, Is.EqualTo(businessRule));
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.FeeId, Is.EqualTo(feeId));
    }

    [Test]
    public void FeeValidationException_WithMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new FeeValidationException(message);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.ValidationErrors, Is.Not.Null);
        Assert.That(exception.ValidationErrors, Is.Empty);
    }

    [Test]
    public void FeeValidationException_WithValidationErrors_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Validation failed";
        var validationErrors = new Dictionary<string, string[]>
        {
            { "Amount", new[] { "Amount must be positive" } },
            { "DueDate", new[] { "Due date cannot be in the past" } }
        };

        // Act
        var exception = new FeeValidationException(message, validationErrors);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.ValidationErrors, Is.EqualTo(validationErrors));
        Assert.That(exception.ValidationErrors.Count, Is.EqualTo(2));
        Assert.That(exception.ValidationErrors.Keys, Does.Contain("Amount"));
        Assert.That(exception.ValidationErrors.Keys, Does.Contain("DueDate"));
    }

    [Test]
    public void FeeValidationException_WithNullValidationErrors_InitializesEmptyDictionary()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new FeeValidationException(message, (Dictionary<string, string[]>?)null);

        // Assert
        Assert.That(exception.Message, Is.EqualTo(message));
        Assert.That(exception.ValidationErrors, Is.Not.Null);
        Assert.That(exception.ValidationErrors, Is.Empty);
    }
}