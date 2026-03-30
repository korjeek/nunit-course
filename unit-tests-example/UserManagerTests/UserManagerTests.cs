using Moq;
using NUnit.Framework;
using UserCreatorTask;
using UserCreatorTask.UserValidators;

namespace UserManagerTests;

public class UserManagerTests
{
    private Mock<IUsersRepository> _usersRepositoryMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<IUserValidator> _userValidatorMock = null!;
    private UserManager _userManager = null!;

    private User _user = null!;

    [SetUp]
    public void Setup()
    {
        _usersRepositoryMock = new Mock<IUsersRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _userValidatorMock = new Mock<IUserValidator>();
            
        _userManager = new UserManager(
            _usersRepositoryMock.Object,
            _emailServiceMock.Object,
            _userValidatorMock.Object);
        
        _user = new User("Apple Banana", "apple123banana", "apple@banana.com", 52);
    }

    [TearDown]
    public void TearDown()
    {
        _usersRepositoryMock.VerifyNoOtherCalls();
        _emailServiceMock.VerifyNoOtherCalls();
        _userValidatorMock.VerifyNoOtherCalls();
    }

    [Test]
    public void CreateNewUser_ShouldBeSuccess_WhenAllIsValid()
    {
        var callOrder = new List<string>();
        var expectedCallOrder = new List<string> { "first", "second", "third", "fourth" };
            
        _userValidatorMock
            .Setup(x => x.Validate(It.IsAny<User>()))
            .Callback(() => callOrder.Add("first"))
            .Returns((true, "ok"));
        _usersRepositoryMock
            .Setup(x => x.GetUser(It.IsAny<string>()))
            .Callback(() => callOrder.Add("second"))
            .Returns((User)null!);
        _usersRepositoryMock
            .Setup(x => x.SaveUser(It.IsAny<User>()))
            .Callback(() => callOrder.Add("third"));
        _emailServiceMock
            .Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => callOrder.Add("fourth"));
            
        _userManager.CreateNewUser(_user);

        Assert.That(callOrder, Is.EqualTo(expectedCallOrder));
        _userValidatorMock.Verify(x => x.Validate(_user), Times.Once);
        _usersRepositoryMock.Verify(x => x.GetUser(_user.Email), Times.Once);
        _usersRepositoryMock.Verify(x => x.SaveUser(_user), Times.Once);
        _emailServiceMock.Verify(x => x.SendEmail(_user.Name, "Welcome", "Thank you for registering!"), Times.Once);
    }

    [Test]
    public void CreateNewUser_ShouldThrowsInvalidUserException_WhenUserIsNotValid()
    {
        _userValidatorMock.Setup(x => x.Validate(It.IsAny<User>())).Returns((false, "error"));

        Assert.That(() => _userManager.CreateNewUser(_user), 
            Throws.InstanceOf<InvalidUserException>().With.Message.EqualTo("error"));
        
        _userValidatorMock.Verify(x => x.Validate(_user), Times.Once);
    }

    [Test]
    public void CreateNewUser_ShouldFails_WhenUserAlreadyExists()
    {
        _userValidatorMock
            .Setup(x => x.Validate(It.IsAny<User>()))
            .Returns((true, "ok"));
        _usersRepositoryMock
            .Setup(x => x.GetUser(It.IsAny<string>()))
            .Returns(_user);

        Assert.That(() => _userManager.CreateNewUser(_user), 
            Throws.InstanceOf<InvalidUserException>().With.Message.EqualTo("UserAlreadyExists"));
        
        _userValidatorMock.Verify(x => x.Validate(_user), Times.Once());
        _usersRepositoryMock.Verify(x => x.GetUser(_user.Email), Times.Once);
    }

    [Test]
    public void DeleteUser_ShouldBeSuccess_WhenAllIsValid()
    {
        var callOrder = new List<string>();
        var expectedCallOrder = new List<string> { "first", "second", "third" };
        
        _usersRepositoryMock
            .Setup(x => x.GetUser(It.IsAny<string>()))
            .Callback(() => callOrder.Add("first"))
            .Returns(_user);
        _usersRepositoryMock
            .Setup(x => x.DeleteUser(It.IsAny<string>()))
            .Callback(() => callOrder.Add("second"));
        _emailServiceMock
            .Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => callOrder.Add("third"));

        _userManager.DeleteUser(_user.Email);
        
        Assert.That(callOrder, Is.EqualTo(expectedCallOrder));
        _usersRepositoryMock.Verify(x => x.GetUser(_user.Email), Times.Once);
        _usersRepositoryMock.Verify(x => x.DeleteUser(_user.Email), Times.Once);
        _emailServiceMock.Verify(x => x.SendEmail(_user.Name, "Goodbye", "Your account has been deleted."), Times.Once);
    }

    [Test]
    public void DeleteUser_ShouldFails_WhenUserNotExists()
    {
        _usersRepositoryMock.Setup(x => x.GetUser(It.IsAny<string>()));

        Assert.That(() => _userManager.DeleteUser(_user.Email), 
            Throws.InvalidOperationException.With.Message.EqualTo("User not found"));
        
        _usersRepositoryMock.Verify(x => x.GetUser(_user.Email), Times.Once);
    }

    [Test]
    public void GetAdultUsers_ShouldOk()
    {
        var youngUser = new User("Walter White", "jesse123pink@man", "young@test.com", 12);
        _usersRepositoryMock.Setup(x => x.GetAllUsers()).Returns([_user, youngUser]);

        var actualUsers = _userManager.GetAdultUsers();

        Assert.That(actualUsers, Is.Not.Null);
        Assert.That(actualUsers.Count, Is.EqualTo(1));
        Assert.That(actualUsers[0], Is.EqualTo(_user));
        _usersRepositoryMock.Verify(x => x.GetAllUsers(), Times.Once);
    }
}