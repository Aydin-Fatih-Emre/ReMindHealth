using Moq;
using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Application.Services.Implementation.Domain;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Tests;

public class NoteServiceTests
{
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly Mock<IUserService> _mockUserService;
	private readonly NoteService _noteService;

	public NoteServiceTests()
	{
		_mockUnitOfWork = new Mock<IUnitOfWork>();
		_mockUserService = new Mock<IUserService>();

		_mockUnitOfWork
			.Setup(x => x.NoteRepository)
			.Returns(Mock.Of<INoteRepository>());

		_noteService = new NoteService(
			_mockUnitOfWork.Object,
			_mockUserService.Object
		);
	}

	// =============================
	// GetNoteAsync
	// =============================

	[Fact]
	public async Task GetNoteAsync_ShouldReturnNote_WhenExists()
	{
		// Arrange
		var noteId = Guid.NewGuid();
		var note = new ExtractedNote
		{
			NoteId = noteId,
			Content = "Test Note"
		};

		var repo = new Mock<INoteRepository>();
		repo.Setup(x => x.GetByIdAsync(noteId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(note);

		_mockUnitOfWork.Setup(x => x.NoteRepository).Returns(repo.Object);

		// Act
		var result = await _noteService.GetNoteAsync(noteId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(noteId, result!.NoteId);
		Assert.Equal("Test Note", result.Content);
	}

	[Fact]
	public async Task GetNoteAsync_ShouldReturnNull_WhenNotFound()
	{
		// Arrange
		var noteId = Guid.NewGuid();

		var repo = new Mock<INoteRepository>();
		repo.Setup(x => x.GetByIdAsync(noteId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((ExtractedNote?)null);

		_mockUnitOfWork.Setup(x => x.NoteRepository).Returns(repo.Object);

		// Act
		var result = await _noteService.GetNoteAsync(noteId);

		// Assert
		Assert.Null(result);
	}

	// =============================
	// GetUserNotesAsync
	// =============================

	[Fact]
	public async Task GetUserNotesAsync_ShouldReturnNotesForCurrentUser()
	{
		// Arrange
		var userId = "user123";
		var notes = new List<ExtractedNote>
		{
			new ExtractedNote { Content = "Note 1", UserId = userId },
			new ExtractedNote { Content = "Note 2", UserId = userId }
		};

		_mockUserService
			.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var repo = new Mock<INoteRepository>();
		repo.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(notes);

		_mockUnitOfWork.Setup(x => x.NoteRepository).Returns(repo.Object);

		// Act
		var result = await _noteService.GetUserNotesAsync();

		// Assert
		Assert.Equal(2, result.Count);
		Assert.All(result, n => Assert.Equal(userId, n.UserId));
	}

	[Fact]
	public async Task GetUserNotesAsync_ShouldReturnEmptyList_WhenNoNotesExist()
	{
		// Arrange
		var userId = "user123";

		_mockUserService
			.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var repo = new Mock<INoteRepository>();
		repo.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<ExtractedNote>());

		_mockUnitOfWork.Setup(x => x.NoteRepository).Returns(repo.Object);

		// Act
		var result = await _noteService.GetUserNotesAsync();

		// Assert
		Assert.Empty(result);
	}

	// =============================
	// GetPinnedNotesAsync
	// =============================

	[Fact]
	public async Task GetPinnedNotesAsync_ShouldReturnPinnedNotes()
	{
		// Arrange
		var userId = "user123";
		var notes = new List<ExtractedNote>
		{
			new ExtractedNote { Content = "Pinned 1", IsPinned = true },
			new ExtractedNote { Content = "Pinned 2", IsPinned = true }
		};

		_mockUserService
			.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var repo = new Mock<INoteRepository>();
		repo.Setup(x => x.GetPinnedByUserIdAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(notes);

		_mockUnitOfWork.Setup(x => x.NoteRepository).Returns(repo.Object);

		// Act
		var result = await _noteService.GetPinnedNotesAsync();

		// Assert
		Assert.Equal(2, result.Count);
		Assert.All(result, n => Assert.True(n.IsPinned));
	}

	// =============================
	// CreateNoteAsync
	// =============================

	[Fact]
	public async Task CreateNoteAsync_ShouldAddNote_AndSave()
	{
		// Arrange
		var note = new ExtractedNote
		{
			Content = "New Note"
		};

		var repo = new Mock<INoteRepository>();
		repo.Setup(x => x.AddAsync(It.IsAny<ExtractedNote>(), It.IsAny<CancellationToken>()))
		.ReturnsAsync((ExtractedNote n, CancellationToken _) => n);


		_mockUnitOfWork.Setup(x => x.NoteRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		var result = await _noteService.CreateNoteAsync(note);

		// Assert
		Assert.Equal(note, result);
		repo.Verify(x => x.AddAsync(note, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	// =============================
	// UpdateNoteAsync
	// =============================

	[Fact]
	public async Task UpdateNoteAsync_ShouldUpdateNote_AndSave()
	{
		// Arrange
		var note = new ExtractedNote
		{
			NoteId = Guid.NewGuid(),
			Content = "Updated"
		};

		var repo = new Mock<INoteRepository>();
		repo.Setup(x => x.UpdateAsync(note, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.NoteRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		await _noteService.UpdateNoteAsync(note);

		// Assert
		repo.Verify(x => x.UpdateAsync(note, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	// =============================
	// DeleteNoteAsync
	// =============================

	[Fact]
	public async Task DeleteNoteAsync_ShouldDeleteNote_AndSave()
	{
		// Arrange
		var noteId = Guid.NewGuid();

		var repo = new Mock<INoteRepository>();
		repo.Setup(x => x.DeleteAsync(noteId, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.NoteRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		await _noteService.DeleteNoteAsync(noteId);

		// Assert
		repo.Verify(x => x.DeleteAsync(noteId, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}
}

