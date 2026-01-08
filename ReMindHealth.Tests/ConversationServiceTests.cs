using Microsoft.Extensions.Logging;
using Moq;
using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Application.Services.Implementation.Domain;
using ReMindHealth.Application.Services.Implementation.External;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Tests;

public class ConversationServiceTests
{
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly Mock<IUserService> _mockUserService;
	private readonly Mock<IExtractionService> _mockExtractionService;
	private readonly Mock<ITranscriptionService> _mockTranscriptionService;
	private readonly Mock<ILogger<ConversationService>> _mockLogger;
	private readonly Mock<IServiceProvider> _mockServiceProvider;

	private readonly ConversationService _service;

	public ConversationServiceTests()
	{
		_mockUnitOfWork = new Mock<IUnitOfWork>();
		_mockUserService = new Mock<IUserService>();
		_mockExtractionService = new Mock<IExtractionService>();
		_mockTranscriptionService = new Mock<ITranscriptionService>();
		_mockLogger = new Mock<ILogger<ConversationService>>();
		_mockServiceProvider = new Mock<IServiceProvider>();

		_mockUnitOfWork.Setup(x => x.ConversationRepository)
			.Returns(Mock.Of<IConversationRepository>());
		_mockUnitOfWork.Setup(x => x.AppointmentRepository)
			.Returns(Mock.Of<IAppointmentRepository>());
		_mockUnitOfWork.Setup(x => x.TaskRepository)
			.Returns(Mock.Of<ITaskRepository>());
		_mockUnitOfWork.Setup(x => x.NoteRepository)
			.Returns(Mock.Of<INoteRepository>());

		_service = new ConversationService(
			_mockUnitOfWork.Object,
			_mockUserService.Object,
			_mockExtractionService.Object,
			_mockTranscriptionService.Object,
			_mockLogger.Object,
			_mockServiceProvider.Object
		);
	}

	// =============================
	// QUERY TESTS
	// =============================

	[Fact]
	public async Task GetConversationAsync_ShouldReturnConversation_WhenExists()
	{
		var id = Guid.NewGuid();
		var conversation = new Conversation { ConversationId = id };

		var repo = new Mock<IConversationRepository>();
		repo.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(conversation);

		_mockUnitOfWork.Setup(x => x.ConversationRepository).Returns(repo.Object);

		var result = await _service.GetConversationAsync(id);

		Assert.NotNull(result);
		Assert.Equal(id, result!.ConversationId);
	}

	[Fact]
	public async Task GetConversationAsync_ShouldReturnNull_WhenNotFound()
	{
		var id = Guid.NewGuid();

		var repo = new Mock<IConversationRepository>();
		repo.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync((Conversation?)null);

		_mockUnitOfWork.Setup(x => x.ConversationRepository).Returns(repo.Object);

		var result = await _service.GetConversationAsync(id);

		Assert.Null(result);
	}

	// =============================
	// USER CONVERSATIONS
	// =============================

	[Fact]
	public async Task GetUserConversationsAsync_ShouldReturnOnlyUserConversations()
	{
		var userId = "user1";
		var list = new List<Conversation>
		{
			new Conversation { UserId = userId },
			new Conversation { UserId = userId }
		};

		_mockUserService.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var repo = new Mock<IConversationRepository>();
		repo.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(list);

		_mockUnitOfWork.Setup(x => x.ConversationRepository).Returns(repo.Object);

		var result = await _service.GetUserConversationsAsync();

		Assert.Equal(2, result.Count);
		Assert.All(result, c => Assert.Equal(userId, c.UserId));
	}

	// =============================
	// CREATE WITH AUDIO
	// =============================

	[Fact]
	public async Task CreateConversationWithAudioAsync_ShouldCreateConversation()
	{
		var userId = "user1";
		var audio = new byte[16000];

		_mockUserService.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var repo = new Mock<IConversationRepository>();
		repo.Setup(x => x.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Conversation c, CancellationToken _) => c);

		repo.Setup(x => x.UpdateAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.ConversationRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockTranscriptionService
			.Setup(x => x.TranscribeFromStreamAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new TranscriptionResult
			{
				Text = "Test",
				Language = "de",
				Confidence = 0.9
			});

		var result = await _service.CreateConversationWithAudioAsync(null, audio);

		Assert.Equal(userId, result.UserId);
		Assert.Equal("Transcribed", result.ProcessingStatus);
		Assert.Equal("Test", result.TranscriptionText);
	}

	// =============================
	// UPDATE
	// =============================

	[Fact]
	public async Task UpdateConversationAsync_ShouldUpdateAndSave()
	{
		var conversation = new Conversation { Title = "Old" };

		var repo = new Mock<IConversationRepository>();
		repo.Setup(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.ConversationRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		await _service.UpdateConversationAsync(conversation);

		repo.Verify(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	// =============================
	// DELETE (SOFT DELETE)
	// =============================

	[Fact]
	public async Task DeleteConversationAsync_ShouldMarkConversationAsDeleted()
	{
		var id = Guid.NewGuid();
		var conversation = new Conversation { ConversationId = id };

		var repo = new Mock<IConversationRepository>();
		repo.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(conversation);
		repo.Setup(x => x.UpdateAsync(conversation, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.ConversationRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		await _service.DeleteConversationAsync(id);

		Assert.True(conversation.IsDeleted);
	}

	// =============================
	// BACKGROUND (nur Stabilität)
	// =============================

	[Fact]
	public async Task ContinueProcessingFromTranscriptionAsync_ShouldNotThrow()
	{
		var id = Guid.NewGuid();

		var ex = await Record.ExceptionAsync(() =>
			_service.ContinueProcessingFromTranscriptionAsync(id));

		Assert.Null(ex);
	}
}
