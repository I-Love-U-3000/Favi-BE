using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Favi_BE.Services;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Favi_BE.ArchitectureTests;

public class PrivacyGuardTests
{
    private class FakeGenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly List<T> _items = new();

        public virtual T GetById(Guid id) => throw new NotImplementedException();
        public virtual Task<T> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public virtual IEnumerable<T> GetAll() => _items;
        public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult<IEnumerable<T>>(_items);
        public virtual IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => throw new NotImplementedException();
        public virtual Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => throw new NotImplementedException();
        public virtual Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => throw new NotImplementedException();
        public virtual void Add(T entity) => _items.Add(entity);
        public virtual void AddRange(IEnumerable<T> entities) => _items.AddRange(entities);
        public virtual Task AddAsync(T entity) { Add(entity); return Task.CompletedTask; }
        public virtual Task AddRangeAsync(IEnumerable<T> entities) { AddRange(entities); return Task.CompletedTask; }
        public virtual void Update(T entity) { }
        public virtual void UpdateRange(IEnumerable<T> entities) { }
        public virtual void Remove(T entity) => _items.Remove(entity);
        public virtual void RemoveRange(IEnumerable<T> entities) { }
        public virtual Task<int> CountAsync() => Task.FromResult(_items.Count);
        public virtual Task<int> CountAsync(Expression<Func<T, bool>> predicate) => throw new NotImplementedException();
    }

    private class FakeProfileRepository : FakeGenericRepository<Profile>, IProfileRepository
    {
        private readonly Dictionary<Guid, Profile> _profiles = new();

        public override void Add(Profile profile)
        {
            base.Add(profile);
            _profiles[profile.Id] = profile;
        }

        public override Task<Profile> GetByIdAsync(Guid id) =>
            Task.FromResult(_profiles.GetValueOrDefault(id)!);

        public override Profile GetById(Guid id) => _profiles.GetValueOrDefault(id)!;

        public Task<Profile> GetByUsernameAsync(string username) => throw new NotImplementedException();
        public Task<IEnumerable<Profile>> GetTopCreatorsAsync(int count) => throw new NotImplementedException();
        public Task<bool> IsUsernameUniqueAsync(string username) => throw new NotImplementedException();
    }

    private class FakeFollowRepository : FakeGenericRepository<Follow>, IFollowRepository
    {
        private readonly HashSet<(Guid Follower, Guid Followee)> _follows = new();

        public void AddFollow(Guid follower, Guid followee) => _follows.Add((follower, followee));

        public Task<bool> IsFollowingAsync(Guid followerId, Guid followedId) =>
            Task.FromResult(_follows.Contains((followerId, followedId)));

        public Task<IEnumerable<Follow>> GetFollowersAsync(Guid profileId, int skip, int take, string? query = null) => throw new NotImplementedException();
        public Task<IEnumerable<Follow>> GetFollowingAsync(Guid profileId, int skip, int take, string? query = null) => throw new NotImplementedException();
        public Task<int> GetFollowersCountAsync(Guid profileId, string? query = null) => throw new NotImplementedException();
        public Task<int> GetFollowingCountAsync(Guid profileId, string? query = null) => throw new NotImplementedException();
        public Task<Follow?> GetAsync(Guid followerId, Guid followeeId) => throw new NotImplementedException();
        public Task<List<Guid>> GetFolloweeIdsAsync(Guid profileId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Guid>> GetFollowerIdsAsync(Guid profileId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private class FakeUnitOfWork : IUnitOfWork
    {
        public FakeProfileRepository ProfilesRepo { get; } = new();
        public FakeFollowRepository FollowsRepo { get; } = new();

        public IProfileRepository Profiles => ProfilesRepo;
        public IFollowRepository Follows => FollowsRepo;

        public IEmailAccountRepository EmailAccounts => null!;
        public IPostRepository Posts => null!;
        public IPostMediaRepository PostMedia => null!;
        public ICollectionRepository Collections => null!;
        public ICommentRepository Comments => null!;
        public ITagRepository Tags => null!;
        public IReportRepository Reports => null!;
        public ISocialLinkRepository SocialLinks => null!;
        public IUserModerationRepository UserModerations => null!;
        public IAdminActionRepository AdminActions => null!;
        public Favi_BE.API.Interfaces.Repositories.IConversationRepository Conversations => null!;
        public Favi_BE.API.Interfaces.Repositories.IMessageRepository Messages => null!;
        public Favi_BE.API.Interfaces.Repositories.INotificationRepository Notifications => null!;

        public IPostTagRepository PostTags => null!;
        public IPostCollectionRepository PostCollections => null!;
        public IUserConversationRepository UserConversations => null!;
        public IStoryRepository Stories => null!;
        public IStoryViewRepository StoryViews => null!;
        public IRepostRepository Reposts => null!;
        public IReactionRepository Reactions => null!;

        public int Complete() => 1;
        public Task<int> CompleteAsync() => Task.FromResult(1);
        public Task BeginTransactionAsync() => Task.CompletedTask;
        public Task CommitTransactionAsync() => Task.CompletedTask;
        public Task RollbackTransactionAsync() => Task.CompletedTask;
        public void Dispose() { }
    }

    [Fact]
    public async Task CanViewPost_PrivatePost_ShouldOnlyBeViewableByOwner()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var guard = new PrivacyGuard(uow);

        var ownerId = Guid.NewGuid();
        var followerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var owner = new Profile { Id = ownerId, Role = UserRole.User, IsBanned = false };
        var follower = new Profile { Id = followerId, Role = UserRole.User, IsBanned = false };
        var stranger = new Profile { Id = strangerId, Role = UserRole.User, IsBanned = false };

        uow.ProfilesRepo.Add(owner);
        uow.ProfilesRepo.Add(follower);
        uow.ProfilesRepo.Add(stranger);
        uow.FollowsRepo.AddFollow(followerId, ownerId);

        var privatePost = new Post
        {
            Id = Guid.NewGuid(),
            ProfileId = ownerId,
            Profile = owner,
            Privacy = PrivacyLevel.Private,
            DeletedDayExpiredAt = null,
            IsArchived = false
        };

        // Act & Assert
        // 1. Owner CAN view their private post
        var ownerCanView = await guard.CanViewPostAsync(privatePost, ownerId);
        ownerCanView.Should().BeTrue();

        // 2. Follower CANNOT view private post
        var followerCanView = await guard.CanViewPostAsync(privatePost, followerId);
        followerCanView.Should().BeFalse();

        // 3. Stranger CANNOT view private post
        var strangerCanView = await guard.CanViewPostAsync(privatePost, strangerId);
        strangerCanView.Should().BeFalse();

        // 4. Anonymous guest CANNOT view private post
        var guestCanView = await guard.CanViewPostAsync(privatePost, null);
        guestCanView.Should().BeFalse();
    }

    [Fact]
    public async Task CanViewPost_FollowersPost_ShouldBeViewableByOwnerAndFollowerOnly()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var guard = new PrivacyGuard(uow);

        var ownerId = Guid.NewGuid();
        var followerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var owner = new Profile { Id = ownerId, Role = UserRole.User, IsBanned = false };
        var follower = new Profile { Id = followerId, Role = UserRole.User, IsBanned = false };
        var stranger = new Profile { Id = strangerId, Role = UserRole.User, IsBanned = false };

        uow.ProfilesRepo.Add(owner);
        uow.ProfilesRepo.Add(follower);
        uow.ProfilesRepo.Add(stranger);
        uow.FollowsRepo.AddFollow(followerId, ownerId);

        var followersPost = new Post
        {
            Id = Guid.NewGuid(),
            ProfileId = ownerId,
            Profile = owner,
            Privacy = PrivacyLevel.Followers,
            DeletedDayExpiredAt = null,
            IsArchived = false
        };

        // Act & Assert
        (await guard.CanViewPostAsync(followersPost, ownerId)).Should().BeTrue();
        (await guard.CanViewPostAsync(followersPost, followerId)).Should().BeTrue();
        (await guard.CanViewPostAsync(followersPost, strangerId)).Should().BeFalse();
        (await guard.CanViewPostAsync(followersPost, null)).Should().BeFalse();
    }

    [Fact]
    public async Task CanViewPost_PublicPost_ShouldBeViewableByEveryone()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var guard = new PrivacyGuard(uow);

        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();

        var owner = new Profile { Id = ownerId, Role = UserRole.User, IsBanned = false };
        var stranger = new Profile { Id = strangerId, Role = UserRole.User, IsBanned = false };

        uow.ProfilesRepo.Add(owner);
        uow.ProfilesRepo.Add(stranger);

        var publicPost = new Post
        {
            Id = Guid.NewGuid(),
            ProfileId = ownerId,
            Profile = owner,
            Privacy = PrivacyLevel.Public,
            DeletedDayExpiredAt = null,
            IsArchived = false
        };

        // Act & Assert
        (await guard.CanViewPostAsync(publicPost, ownerId)).Should().BeTrue();
        (await guard.CanViewPostAsync(publicPost, strangerId)).Should().BeTrue();
        (await guard.CanViewPostAsync(publicPost, null)).Should().BeTrue();
    }

    [Fact]
    public async Task CanViewPost_ArchivedOrDeletedPost_ShouldNotBeViewableByOtherUsers()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var guard = new PrivacyGuard(uow);

        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var owner = new Profile { Id = ownerId, Role = UserRole.User, IsBanned = false };
        var otherUser = new Profile { Id = otherUserId, Role = UserRole.User, IsBanned = false };

        uow.ProfilesRepo.Add(owner);
        uow.ProfilesRepo.Add(otherUser);

        var archivedPost = new Post
        {
            Id = Guid.NewGuid(),
            ProfileId = ownerId,
            Profile = owner,
            Privacy = PrivacyLevel.Public,
            IsArchived = true,
            DeletedDayExpiredAt = null
        };

        var deletedPost = new Post
        {
            Id = Guid.NewGuid(),
            ProfileId = ownerId,
            Profile = owner,
            Privacy = PrivacyLevel.Public,
            IsArchived = false,
            DeletedDayExpiredAt = DateTime.UtcNow.AddDays(30)
        };

        // Act & Assert
        (await guard.CanViewPostAsync(archivedPost, otherUserId)).Should().BeFalse();
        (await guard.CanViewPostAsync(deletedPost, otherUserId)).Should().BeFalse();
        (await guard.CanViewPostAsync(archivedPost, ownerId)).Should().BeTrue();
        (await guard.CanViewPostAsync(deletedPost, ownerId)).Should().BeTrue();
    }
}
