using Favi_BE.Data.Repositories;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Interfaces;
using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Data.Repositories;

namespace Favi_BE.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction _transaction;
        private bool _disposed = false;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;

            // Initialize repositories
            Profiles = new ProfileRepository(_context);
            Posts = new PostRepository(_context);
            PostMedia = new PostMediaRepository(_context);
            Collections = new CollectionRepository(_context);
            Comments = new CommentRepository(_context);
            Tags = new TagRepository(_context);
            Reports = new ReportRepository(_context);
            SocialLinks = new SocialLinkRepository(_context);
            Conversations = new ConversationRepository(_context);
            Messages = new MessageRepository(_context);

            // Join tables
            PostTags = new PostTagRepository(_context);
            PostCollections = new PostCollectionRepository(_context);
            Follows = new FollowRepository(_context);
            Reactions = new ReactionRepository(_context);
            UserConversations = new UserConversationRepository(_context);
        }

        // Core entities
        public IProfileRepository Profiles { get; private set; }
        public IPostRepository Posts { get; private set; }
        public IPostMediaRepository PostMedia { get; private set; }
        public ICollectionRepository Collections { get; private set; }
        public ICommentRepository Comments { get; private set; }
        public ITagRepository Tags { get; private set; }
        public IReportRepository Reports { get; private set; }
        public ISocialLinkRepository SocialLinks { get; private set; }
        public IConversationRepository Conversations { get; private set; }
        public IMessageRepository Messages { get; private set; }

        // Join tables
        public IPostTagRepository PostTags { get; private set; }
        public IPostCollectionRepository PostCollections { get; private set; }
        public IFollowRepository Follows { get; private set; }
        public IReactionRepository Reactions { get; private set; }
        public IUserConversationRepository UserConversations { get; private set; }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _transaction.CommitAsync();
            }
            catch
            {
                await _transaction.RollbackAsync();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _context.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}