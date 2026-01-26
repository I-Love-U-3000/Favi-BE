using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.Interfaces.Repositories;
using System;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Core entities
        IProfileRepository Profiles { get; }
        IEmailAccountRepository EmailAccounts { get; }
        IPostRepository Posts { get; }
        IPostMediaRepository PostMedia { get; }
        ICollectionRepository Collections { get; }
        ICommentRepository Comments { get; }
        ITagRepository Tags { get; }
        IReportRepository Reports { get; }
        ISocialLinkRepository SocialLinks { get; }
        IUserModerationRepository UserModerations { get; }
        IAdminActionRepository AdminActions { get; }
        IConversationRepository Conversations { get; }
        IMessageRepository Messages { get; }
        INotificationRepository Notifications { get; }

        // Join tables and relationships
        IPostTagRepository PostTags { get; }
        IPostCollectionRepository PostCollections { get; }
        IFollowRepository Follows { get; }
        IReactionRepository Reactions { get; }
        IUserConversationRepository UserConversations { get; }
        IStoryRepository Stories { get; }
        IStoryViewRepository StoryViews { get; }
        IRepostRepository Reposts { get; }

        // Transaction management
        int Complete();
        Task<int> CompleteAsync();

        // Transaction control
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
