using NUnit.Framework;
using UserCreatorTask.UserValidators;

namespace UserValidatorTests;

[Parallelizable(ParallelScope.Children)]
public class PasswordValidatorTests
{
    private readonly PasswordValidator _validator = new();

    [Test]
    public void IsValid_ShouldReturnTrue_WhenPasswordMeetsAllRequirements()
    {
        var password = new string('a', 95) + "A322?!";
        var result = _validator.IsValid(password);
        Assert.That(result, Is.True, "Expected True for a valid 100-character password.");
    }
    
    [Test]
    public void IsValid_ShouldReturnFalse_WhenPasswordContainsNonEnglishCharacters()
    {
        var password = new string('Ъ', 95) + "Ъ322!?";
        var result = _validator.IsValid(password);
        Assert.That(result, Is.False, "Expected False for non-English (Cyrillic) characters.");
    }

    [Test]
    public void IsValid_ShouldReturnFalse_WhenPasswordIsTooShort()
    {
        var password = "a1A!" + new string('a', 52);
        var result = _validator.IsValid(password);
        Assert.That(result, Is.False, "Expected False for password shorter than 100 characters.");
    }
    
    [Test]
    public void IsValid_ShouldReturnFalse_WhenPasswordMissingSpecialSymbols()
    {
        var password = new string('a', 96) + "A322";
        var result = _validator.IsValid(password);
        Assert.That(result, Is.False, "Expected False when special symbols are missing.");
    }

    [Test]
    public void IsValid_ShouldReturnTrue_WhenPasswordContainsSpaces()
    {
        var password = new string('a', 93) + " A322!?"; 
        var result = _validator.IsValid(password);
        Assert.That(result, Is.True, "Expected False when password contains spaces (based on current Regex).");
    }

    [Test]
    public void IsValid_ShouldReturnFalse_WhenPasswordMissingUppercase()
    {
        var password = new string('a', 96) + "322?!";
        var result = _validator.IsValid(password);
        Assert.That(result, Is.False, "Expected False when uppercase letters are missing.");
    }

    [Test]
    public void IsValid_ShouldReturnFalse_WhenPasswordMissingLowercase()
    {
        var password = new string('A', 96) + "322?!";
        var result = _validator.IsValid(password);
        Assert.That(result, Is.False, "Expected False when lowercase letters are missing.");
    }

    [Test]
    public void IsValid_ShouldReturnFalse_WhenPasswordMissingNumbers()
    {
        var password = new string('a', 97) + "ww!";
        var result = _validator.IsValid(password);
        Assert.That(result, Is.False, "Expected False when numbers are missing.");
    }

    [Test]
    [NonParallelizable]
    public void IsValid_ShouldThrowArgumentNullException_WhenValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _validator.IsValid(null), 
            "Expected ArgumentNullException when the input is null.");
    }
}