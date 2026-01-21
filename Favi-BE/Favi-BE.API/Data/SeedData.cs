using Favi_BE.API.Models.Entities;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.API.Models.Enums;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Data
{
    public class SeedData
    {
        private static readonly Random _random = new Random(42); // Fixed seed for reproducibility

        // Sample data for realistic generation
        private static readonly string[] _usernames = [
            "alex_photo", "sarah_art", "mike_travel", "emma_food", "david_tech",
            "lisa_fitness", "john_music", "anna_style", "chris_gaming", "nina_books",
            "tom_films", "kate_science", "steve_cars", "amy_design", "pete_sports",
            "julia_coffee", "mark_pizza", "rachel_comedy", "dan_history", "zoe_nature"
        ];

        private static readonly string[] _displayNames = [
            "Alex Johnson", "Sarah Williams", "Mike Brown", "Emma Davis", "David Miller",
            "Lisa Wilson", "John Taylor", "Anderson Moore", "Chris Jackson", "Nina White",
            "Tom Harris", "Kate Martin", "Steve Thompson", "Amy Garcia", "Peter Martinez",
            "Julia Robinson", "Mark Clark", "Rachel Lewis", "Dan Lee", "Zoe Walker"
        ];

        private static readonly string[] _bios = [
            "Photography enthusiast | Nature lover | Coffee addict ☕",
            "Digital artist | Creative soul | Living my best life 🎨",
            "World traveler ✈️ | 50+ countries and counting | Adventure awaits!",
            "Foodie 🍕 | Home chef | Restaurant explorer",
            "Tech enthusiast | Coding life | Building the future 💻",
            "Fitness junkie 💪 | Gym rat | Healthy living advocate",
            "Music producer 🎵 | Sound engineer | Audiophile",
            "Fashionista 👗 | Style inspiration | Shopping therapy",
            "Gamer 🎮 | Streamer | Level 99 nerd",
            "Bookworm 📚 | Reading is my passion | Library regular",
            "Movie buff 🎬 | Film critic | Cinema lover",
            "Science nerd 🔬 | Researcher | Always learning",
            "Car enthusiast 🚗 | Speed demon | Formula 1 fan",
            "UX designer | Creative mind | Pixel perfect",
            "Sports fan ⚽ | Live for the game | Team player",
            "Coffee lover ☕ | Barista wannabe | Caffeine powered",
            "Pizza connoisseur 🍕 | Slice of life | Food critic",
            "Comedy fan 😂 | Making people laugh | Good vibes only",
            "History buff 📜 | Past meets present | Time traveler",
            "Nature photographer 🌿 | Outdoor adventurer | Wild at heart"
        ];

        private static readonly string[] _postCaptions = [
            "Beautiful sunset today! 🌅 #sunset #nature",
            "New recipe I tried turned out amazing! 🍝 #food #cooking",
            "Morning workout done! 💪 Who else is grinding today?",
            "This view is incredible! Can't believe I'm actually here 😍",
            "Productive day at the office. Love what I do! 💼",
            "Weekend vibes with my favorite people! 🎉",
            "Trying out this new café. The coffee is fantastic! ☕",
            "Just finished this book. Highly recommend! 📚",
            "New personal record at the gym today! 🏋️",
            "Art therapy session was so relaxing today 🎨",
            "Random throwback to happier times! #tbt #memories",
            "Can't wait for the weekend! Any plans? 🤔",
            "This weather is perfect! ☀️ #summer #vibes",
            "Home sweet home 🏠 Nothing beats relaxing after a long day",
            "New haircut, new me! ✂️ What do you think?",
            "Late night coding session 💻 Building something cool!",
            "Exploring the city today! So many hidden gems 🏙️",
            "Movie night! 🍿 Any recommendations?",
            "Breakfast of champions! 🥗 Healthy living starts here",
            "Feeling grateful today! ❤️ #blessed #thankful"
        ];

        private static readonly string[] _commentContents = [
            "This is amazing! 😍",
            "Love this so much! ❤️",
            "Incredible shot! 📸",
            "Wow, just wow!",
            "This made my day! 🌟",
            "Absolutely stunning!",
            "You're so talented! 👏",
            "This is everything!",
            "Goals! 🙌",
            "Can't get enough of this!",
            "This is exactly what I needed to see today!",
            "You never disappoint!",
            "This is pure gold! ✨",
            "How do you do it?!",
            "This deserves way more attention!",
            "I'm obsessed! 😍",
            "This is art! 🎨",
            "Brilliant! Simply brilliant!",
            "You've outdone yourself!",
            "This speaks to my soul! 💫"
        ];

        private static readonly string[] _tags = [
            "photography", "art", "travel", "food", "technology", "fitness",
            "music", "fashion", "gaming", "books", "movies", "science",
            "cars", "design", "sports", "coffee", "pizza", "comedy", "history", "nature",
            "sunset", "nature", "cooking", "lifestyle", "motivation", "coffee", "reading",
            "workout", "creative", "tbt", "summer", "relaxing", "style", "coding", "city",
            "movies", "healthy", "grateful", "beautiful", "amazing", "love", "talent"
        ];

        private static readonly string[] _collectionTitles = [
            "My Favorites", "Inspiration", "Dream Destinations", "Food Adventures",
            "Fitness Journey", "Art Collection", "Music Vibes", "Fashion Goals",
            "Gaming Setup", "Book Reviews", "Movie Nights", "Science Facts",
            "Dream Cars", "Design Inspiration", "Sports Highlights", "Coffee Spots",
            "Pizza Reviews", "Funny Moments", "Historical Places", "Nature Shots"
        ];

        private static readonly SocialKind[] _socialKinds = [
            SocialKind.Instagram, SocialKind.Twitter, SocialKind.Facebook, SocialKind.Tiktok,
            SocialKind.Youtube, SocialKind.LinkedIn, SocialKind.Github
        ];

        // Placeholder image URLs using Unsplash for realistic images
        private static readonly string[] _avatarUrls = [
            "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1599566150163-29194dcabd36?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1527980965255-d3b416303d12?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=200&h=200&fit=crop",
            "https://images.unsplash.com/photo-1544725176-7c40e5a71c5e?w=200&h=200&fit=crop"
        ];

        private static readonly string[] _postImageUrls = [
            "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=800&fit=crop", // Mountain
            "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800&h=600&fit=crop", // Food
            "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=800&h=600&fit=crop", // Gym
            "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=800&h=600&fit=crop", // Travel
            "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=800&h=600&fit=crop", // Work
            "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&h=600&fit=crop", // People
            "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=800&h=600&fit=crop", // Coffee
            "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=800&h=600&fit=crop", // Book
            "https://images.unsplash.com/photo-1571902943202-507ec2618e8f?w=800&h=600&fit=crop", // Gym2
            "https://images.unsplash.com/photo-1549490349-8643362247b5?w=800&h=600&fit=crop", // Art
            "https://images.unsplash.com/photo-1501785888041-af3ef285b470?w=800&h=600&fit=crop", // Nature2
            "https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&h=600&fit=crop", // Beach
            "https://images.unsplash.com/photo-1556912173-3db996ea0642?w=800&h=600&fit=crop", // Tech
            "https://images.unsplash.com/photo-1496747611176-843222e1e57c?w=800&h=600&fit=crop", // City
            "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=800&h=600&fit=crop", // Movie
            "https://images.unsplash.com/photo-1511884642898-4c92249e20b6?w=800&h=600&fit=crop", // Music
            "https://images.unsplash.com/photo-1441986300917-64674bd600d8?w=800&h=600&fit=crop", // Shopping
            "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800&h=600&fit=crop", // Party
            "https://images.unsplash.com/photo-1501604892684-9b4e8f3b8855?w=800&h=600&fit=crop", // Home
            "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=800&h=600&fit=crop"  // Portrait
        ];

        private static readonly string[] _coverUrls = [
            "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=1200&h=400&fit=crop",
            "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=1200&h=400&fit=crop",
            "https://images.unsplash.com/photo-1470071459604-3b5ec3a7fe05?w=1200&h=400&fit=crop",
            "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=1200&h=400&fit=crop",
            "https://images.unsplash.com/photo-1472214103451-9374bd1c798e?w=1200&h=400&fit=crop"
        ];

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if there are any existing profiles
            var hasExistingData = await context.Profiles.AnyAsync();
            if (hasExistingData)
            {
                Console.WriteLine("[SeedData] Database already seeded. Skipping...");
                return;
            }

            Console.WriteLine("[SeedData] No existing data found. Starting seed process...");

            // Clear all existing data (for safety)
            await ClearDatabaseAsync(context);

            // Seed in proper order respecting foreign keys
            var profiles = await SeedProfilesAsync(context);
            await SeedEmailAccountsAsync(context, profiles);
            await SeedTagsAsync(context);
            var posts = await SeedPostsAsync(context, profiles);
            await SeedPostMediaAsync(context, posts);
            await SeedPostTagsAsync(context, posts);
            var stories = await SeedStoriesAsync(context, profiles);
            await SeedStoryViewsAsync(context, stories, profiles);
            var collections = await SeedCollectionsAsync(context, profiles);
            await SeedPostCollectionsAsync(context, posts, collections);
            var comments = await SeedCommentsAsync(context, posts, profiles);
            await SeedReactionsAsync(context, posts, comments, collections, profiles);
            var reposts = await SeedRepostsAsync(context, posts, profiles);
            await SeedFollowsAsync(context, profiles);
            await SeedSocialLinksAsync(context, profiles);
            await SeedConversationsAsync(context, profiles);
            await SeedNotificationsAsync(context, profiles, posts, comments);

            Console.WriteLine("[SeedData] Database seeded successfully!");
        }

        private static async Task ClearDatabaseAsync(AppDbContext context)
        {
            Console.WriteLine("[SeedData] Clearing existing data...");

            // Order matters - delete children first, then parents
            await context.Notifications.ExecuteDeleteAsync();
            await context.MessageReads.ExecuteDeleteAsync();
            await context.Messages.ExecuteDeleteAsync();
            await context.UserConversations.ExecuteDeleteAsync();
            await context.Conversations.ExecuteDeleteAsync();
            await context.Reactions.ExecuteDeleteAsync();
            await context.Reposts.ExecuteDeleteAsync();
            await context.StoryViews.ExecuteDeleteAsync();
            await context.Stories.ExecuteDeleteAsync();
            await context.PostTags.ExecuteDeleteAsync();
            await context.PostCollections.ExecuteDeleteAsync();
            await context.PostMedias.ExecuteDeleteAsync();
            await context.Comments.ExecuteDeleteAsync();
            await context.Posts.ExecuteDeleteAsync();
            await context.Collections.ExecuteDeleteAsync();
            await context.SocialLinks.ExecuteDeleteAsync();
            await context.Follows.ExecuteDeleteAsync();
            await context.Tags.ExecuteDeleteAsync();
            await context.EmailAccounts.ExecuteDeleteAsync();
            await context.Profiles.ExecuteDeleteAsync();

            Console.WriteLine("[SeedData] Database cleared.");
        }

        private static async Task<List<Profile>> SeedProfilesAsync(AppDbContext context)
        {
            Console.WriteLine("[SeedData] Seeding Profiles...");
            var profiles = new List<Profile>();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 20; i++)
            {
                var createdAt = now.AddDays(-_random.Next(1, 365)).AddHours(-_random.Next(0, 24));
                var profile = new Profile
                {
                    Id = Guid.NewGuid(),
                    Username = _usernames[i],
                    DisplayName = _displayNames[i],
                    Bio = _bios[i],
                    AvatarUrl = _avatarUrls[i % _avatarUrls.Length],
                    CoverUrl = _coverUrls[i % _coverUrls.Length],
                    Role = i == 0 ? UserRole.Admin : (i < 3 ? UserRole.Moderator : UserRole.User),
                    CreatedAt = createdAt,
                    LastActiveAt = now.AddHours(-_random.Next(0, 48)),
                    PrivacyLevel = i < 15 ? PrivacyLevel.Public : PrivacyLevel.Private,
                    FollowPrivacyLevel = PrivacyLevel.Public,
                    IsBanned = false
                };
                profiles.Add(profile);
            }

            await context.Profiles.AddRangeAsync(profiles);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {profiles.Count} profiles.");
            return profiles;
        }

        private static async Task SeedEmailAccountsAsync(AppDbContext context, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding EmailAccounts...");
            var emailAccounts = new List<EmailAccount>();

            foreach (var profile in profiles)
            {
                var email = $"{profile.Username}@example.com";
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"); // Default password for testing

                var emailAccount = new EmailAccount
                {
                    Id = profile.Id, // Same ID as profile
                    Email = email,
                    PasswordHash = passwordHash,
                    CreatedAt = profile.CreatedAt,
                    EmailVerifiedAt = profile.CreatedAt.AddMinutes(5)
                };
                emailAccounts.Add(emailAccount);
            }

            await context.EmailAccounts.AddRangeAsync(emailAccounts);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {emailAccounts.Count} email accounts.");
        }

        private static async Task SeedTagsAsync(AppDbContext context)
        {
            Console.WriteLine("[SeedData] Seeding Tags...");
            var tags = new List<Tag>();

            foreach (var tagName in _tags.Take(20))
            {
                var tag = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = tagName
                };
                tags.Add(tag);
            }

            await context.Tags.AddRangeAsync(tags);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {tags.Count} tags.");
        }

        private static async Task<List<Post>> SeedPostsAsync(AppDbContext context, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding Posts...");
            var posts = new List<Post>();
            var now = DateTime.UtcNow;

            // Create 20 posts distributed among users
            for (int i = 0; i < 20; i++)
            {
                var profile = profiles[i % profiles.Count];
                var createdAt = now.AddDays(-_random.Next(1, 60)).AddHours(-_random.Next(0, 24));

                var post = new Post
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profile.Id,
                    Caption = _postCaptions[i % _postCaptions.Length],
                    Privacy = i < 18 ? PrivacyLevel.Public : PrivacyLevel.Private,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    LocationName = _random.Next(100) < 30 ? $"Location {i + 1}" : null,
                    LocationFullAddress = _random.Next(100) < 30 ? $"{i + 1} Main Street, City" : null,
                    LocationLatitude = _random.Next(100) < 30 ? 40.7128 + (_random.NextDouble() - 0.5) * 0.1 : null,
                    LocationLongitude = _random.Next(100) < 30 ? -74.0060 + (_random.NextDouble() - 0.5) * 0.1 : null,
                    IsArchived = false,
                    IsNSFW = false
                };
                posts.Add(post);
            }

            await context.Posts.AddRangeAsync(posts);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {posts.Count} posts.");
            return posts;
        }

        private static async Task SeedPostMediaAsync(AppDbContext context, List<Post> posts)
        {
            Console.WriteLine("[SeedData] Seeding PostMedias...");
            var postMedias = new List<PostMedia>();

            foreach (var post in posts)
            {
                var media = new PostMedia
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    ProfileId = post.ProfileId,
                    Url = _postImageUrls[_random.Next(_postImageUrls.Length)],
                    ThumbnailUrl = _postImageUrls[_random.Next(_postImageUrls.Length)],
                    Position = 0,
                    PublicId = $"seed_{Guid.NewGuid()}",
                    Width = 800,
                    Height = _random.Next(600, 801),
                    Format = "jpg",
                    IsAvatar = false,
                    IsPoster = false
                };
                postMedias.Add(media);
            }

            await context.PostMedias.AddRangeAsync(postMedias);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {postMedias.Count} post medias.");
        }

        private static async Task SeedPostTagsAsync(AppDbContext context, List<Post> posts)
        {
            Console.WriteLine("[SeedData] Seeding PostTags...");
            var postTags = new List<PostTag>();
            var tags = await context.Tags.Take(15).ToListAsync();

            foreach (var post in posts)
            {
                // Add 1-3 random tags to each post
                var numTags = _random.Next(1, 4);
                var shuffledTags = tags.OrderBy(x => _random.Next()).Take(numTags);

                foreach (var tag in shuffledTags)
                {
                    var postTag = new PostTag
                    {
                        PostId = post.Id,
                        TagId = tag.Id
                    };
                    postTags.Add(postTag);
                }
            }

            await context.PostTags.AddRangeAsync(postTags);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {postTags.Count} post tags.");
        }

        private static async Task<List<Story>> SeedStoriesAsync(AppDbContext context, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding Stories...");
            var stories = new List<Story>();
            var now = DateTime.UtcNow;

            // Create 20 stories from the most active users
            for (int i = 0; i < 20; i++)
            {
                var profile = profiles[i % (profiles.Count / 2)]; // Only first half users post stories
                var createdAt = now.AddHours(-_random.Next(1, 24)); // Stories are recent (last 24 hours)
                var expiresAt = createdAt.AddHours(24); // Stories expire after 24 hours

                var story = new Story
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profile.Id,
                    MediaUrl = _postImageUrls[i % _postImageUrls.Length],
                    MediaPublicId = $"story_{Guid.NewGuid()}",
                    MediaWidth = 1080,
                    MediaHeight = 1920, // Portrait format for stories
                    MediaFormat = "jpg",
                    ThumbnailUrl = _postImageUrls[i % _postImageUrls.Length],
                    Privacy = i < 18 ? PrivacyLevel.Public : PrivacyLevel.Private,
                    IsArchived = false,
                    IsNSFW = false,
                    CreatedAt = createdAt,
                    ExpiresAt = expiresAt
                };
                stories.Add(story);
            }

            await context.Stories.AddRangeAsync(stories);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {stories.Count} stories.");
            return stories;
        }

        private static async Task SeedStoryViewsAsync(AppDbContext context, List<Story> stories, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding StoryViews...");
            var storyViews = new List<StoryView>();

            foreach (var story in stories)
            {
                // Random viewers for each story (excluding the story owner)
                var potentialViewers = profiles.Where(p => p.Id != story.ProfileId).ToList();
                var numViewers = _random.Next(1, Math.Min(10, potentialViewers.Count + 1));

                foreach (var viewer in potentialViewers.OrderBy(x => _random.Next()).Take(numViewers))
                {
                    var viewedAt = story.CreatedAt.AddMinutes(_random.Next(1, 60));
                    var storyView = new StoryView
                    {
                        StoryId = story.Id,
                        ViewerProfileId = viewer.Id,
                        ViewedAt = viewedAt
                    };
                    storyViews.Add(storyView);
                }
            }

            await context.StoryViews.AddRangeAsync(storyViews);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {storyViews.Count} story views.");
        }

        private static async Task<List<Collection>> SeedCollectionsAsync(AppDbContext context, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding Collections...");
            var collections = new List<Collection>();
            var now = DateTime.UtcNow;

            // Create 20 collections
            for (int i = 0; i < 20; i++)
            {
                var profile = profiles[i % profiles.Count];
                var createdAt = now.AddDays(-_random.Next(1, 180));

                var collection = new Collection
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profile.Id,
                    Title = _collectionTitles[i % _collectionTitles.Length],
                    Description = $"A collection of my favorite {_collectionTitles[i % _collectionTitles.Length].ToLower()} posts",
                    CoverImageUrl = _postImageUrls[i % _postImageUrls.Length],
                    CoverImagePublicId = $"collection_{Guid.NewGuid()}",
                    PrivacyLevel = i < 17 ? PrivacyLevel.Public : PrivacyLevel.Private,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                };
                collections.Add(collection);
            }

            await context.Collections.AddRangeAsync(collections);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {collections.Count} collections.");
            return collections;
        }

        private static async Task SeedPostCollectionsAsync(AppDbContext context, List<Post> posts, List<Collection> collections)
        {
            Console.WriteLine("[SeedData] Seeding PostCollections...");
            var postCollections = new List<PostCollection>();

            // Add random posts to collections
            foreach (var collection in collections)
            {
                var numPosts = _random.Next(2, 8);
                var randomPosts = posts.OrderBy(x => _random.Next()).Take(numPosts);

                foreach (var post in randomPosts)
                {
                    var postCollection = new PostCollection
                    {
                        PostId = post.Id,
                        CollectionId = collection.Id
                    };
                    postCollections.Add(postCollection);
                }
            }

            await context.PostCollections.AddRangeAsync(postCollections);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {postCollections.Count} post collections.");
        }

        private static async Task<List<Comment>> SeedCommentsAsync(AppDbContext context, List<Post> posts, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding Comments...");
            var comments = new List<Comment>();
            var now = DateTime.UtcNow;

            foreach (var post in posts)
            {
                var numComments = _random.Next(1, 6);
                var randomProfiles = profiles.Where(p => p.Id != post.ProfileId).OrderBy(x => _random.Next()).Take(numComments);

                foreach (var profile in randomProfiles)
                {
                    var createdAt = post.CreatedAt.AddMinutes(_random.Next(1, 1440)); // Comments within 24 hours of post

                    var comment = new Comment
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        ProfileId = profile.Id,
                        Content = _commentContents[_random.Next(_commentContents.Length)],
                        CreatedAt = createdAt,
                        UpdatedAt = null
                    };
                    comments.Add(comment);

                    // Occasionally add replies
                    if (_random.Next(100) < 30) // 30% chance of replies
                    {
                        var numReplies = _random.Next(1, 3);
                        var repliers = profiles.Where(p => p.Id != profile.Id && p.Id != post.ProfileId).OrderBy(x => _random.Next()).Take(numReplies);

                        foreach (var replier in repliers)
                        {
                            var replyCreatedAt = createdAt.AddMinutes(_random.Next(1, 60));
                            var reply = new Comment
                            {
                                Id = Guid.NewGuid(),
                                PostId = post.Id,
                                ProfileId = replier.Id,
                                Content = _commentContents[_random.Next(_commentContents.Length)],
                                ParentCommentId = comment.Id,
                                CreatedAt = replyCreatedAt,
                                UpdatedAt = null
                            };
                            comments.Add(reply);
                        }
                    }
                }
            }

            await context.Comments.AddRangeAsync(comments);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {comments.Count} comments.");
            return comments;
        }

        private static async Task SeedReactionsAsync(AppDbContext context, List<Post> posts, List<Comment> comments, List<Collection> collections, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding Reactions...");
            var reactions = new List<Reaction>();
            var reactionTypes = Enum.GetValues<ReactionType>();

            // Reactions on posts
            foreach (var post in posts)
            {
                var numReactions = _random.Next(2, 15);
                var reactors = profiles.Where(p => p.Id != post.ProfileId).OrderBy(x => _random.Next()).Take(numReactions);

                foreach (var reactor in reactors)
                {
                    var reaction = new Reaction
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        ProfileId = reactor.Id,
                        Type = reactionTypes[_random.Next(reactionTypes.Length)],
                        CreatedAt = post.CreatedAt.AddMinutes(_random.Next(1, 1440))
                    };
                    reactions.Add(reaction);
                }
            }

            // Reactions on some comments (30% of comments)
            foreach (var comment in comments.OrderBy(x => _random.Next()).Take((int)(comments.Count * 0.3)))
            {
                var numReactions = _random.Next(1, 5);
                var reactors = profiles.Where(p => p.Id != comment.ProfileId).OrderBy(x => _random.Next()).Take(numReactions);

                foreach (var reactor in reactors)
                {
                    var reaction = new Reaction
                    {
                        Id = Guid.NewGuid(),
                        CommentId = comment.Id,
                        ProfileId = reactor.Id,
                        Type = reactionTypes[_random.Next(reactionTypes.Length)],
                        CreatedAt = comment.CreatedAt.AddMinutes(_random.Next(1, 60))
                    };
                    reactions.Add(reaction);
                }
            }

            // Reactions on some collections (40% of collections)
            foreach (var collection in collections.OrderBy(x => _random.Next()).Take((int)(collections.Count * 0.4)))
            {
                var numReactions = _random.Next(1, 8);
                var reactors = profiles.Where(p => p.Id != collection.ProfileId).OrderBy(x => _random.Next()).Take(numReactions);

                foreach (var reactor in reactors)
                {
                    var reaction = new Reaction
                    {
                        Id = Guid.NewGuid(),
                        CollectionId = collection.Id,
                        ProfileId = reactor.Id,
                        Type = reactionTypes[_random.Next(reactionTypes.Length)],
                        CreatedAt = collection.CreatedAt.AddMinutes(_random.Next(1, 1440))
                    };
                    reactions.Add(reaction);
                }
            }

            await context.Reactions.AddRangeAsync(reactions);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {reactions.Count} reactions.");
        }

        private static async Task<List<Repost>> SeedRepostsAsync(AppDbContext context, List<Post> posts, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding Reposts...");
            var reposts = new List<Repost>();
            var now = DateTime.UtcNow;

            // Create 20 reposts
            for (int i = 0; i < 20; i++)
            {
                var originalPost = posts[i % posts.Count];
                var resharer = profiles[(i + 5) % profiles.Count]; // Different user from original poster

                // Skip if this profile already reposted this post
                if (await context.Reposts.AnyAsync(r => r.ProfileId == resharer.Id && r.OriginalPostId == originalPost.Id))
                    continue;

                var createdAt = originalPost.CreatedAt.AddHours(_random.Next(1, 48));

                var repost = new Repost
                {
                    Id = Guid.NewGuid(),
                    ProfileId = resharer.Id,
                    OriginalPostId = originalPost.Id,
                    Caption = _random.Next(100) < 50 ? _postCaptions[i % _postCaptions.Length] : null,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                };
                reposts.Add(repost);
            }

            await context.Reposts.AddRangeAsync(reposts);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {reposts.Count} reposts.");
            return reposts;
        }

        private static async Task SeedFollowsAsync(AppDbContext context, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding Follows...");
            var follows = new List<Follow>();
            var now = DateTime.UtcNow;

            // Create follow relationships
            // Each user follows 3-10 other users
            foreach (var follower in profiles)
            {
                var numFollows = _random.Next(3, 11);
                var potentialFollowees = profiles.Where(p => p.Id != follower.Id).ToList();
                var followees = potentialFollowees.OrderBy(x => _random.Next()).Take(numFollows);

                foreach (var followee in followees)
                {
                    var follow = new Follow
                    {
                        FollowerId = follower.Id,
                        FolloweeId = followee.Id,
                        CreatedAt = now.AddDays(-_random.Next(1, 365))
                    };
                    follows.Add(follow);
                }
            }

            await context.Follows.AddRangeAsync(follows);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {follows.Count} follows.");
        }

        private static async Task SeedSocialLinksAsync(AppDbContext context, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding SocialLinks...");
            var socialLinks = new List<SocialLink>();

            // Add 1-3 social links to each profile
            foreach (var profile in profiles)
            {
                var numLinks = _random.Next(1, 4);
                var kinds = _socialKinds.OrderBy(x => _random.Next()).Take(numLinks);

                foreach (var kind in kinds)
                {
                    var platform = kind.ToString().ToLower();
                    var link = new SocialLink
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = profile.Id,
                        Kind = kind,
                        Url = $"https://{platform}.com/{profile.Username}",
                        CreatedAt = profile.CreatedAt
                    };
                    socialLinks.Add(link);
                }
            }

            await context.SocialLinks.AddRangeAsync(socialLinks);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {socialLinks.Count} social links.");
        }

        private static async Task SeedConversationsAsync(AppDbContext context, List<Profile> profiles)
        {
            Console.WriteLine("[SeedData] Seeding Conversations and Messages...");
            var now = DateTime.UtcNow;
            var conversationCount = 0;
            var messageCount = 0;

            // Create 10 DM conversations between random pairs
            for (int i = 0; i < 10; i++)
            {
                var user1 = profiles[i * 2 % profiles.Count];
                var user2 = profiles[(i * 2 + 1) % profiles.Count];

                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = ConversationType.Dm,
                    CreatedAt = now.AddDays(-_random.Next(1, 60)),
                    LastMessageAt = now.AddHours(-_random.Next(1, 48))
                };

                // Add users to conversation
                var userConvo1 = new UserConversation
                {
                    ConversationId = conversation.Id,
                    ProfileId = user1.Id,
                    Role = "member",
                    JoinedAt = conversation.CreatedAt
                };
                var userConvo2 = new UserConversation
                {
                    ConversationId = conversation.Id,
                    ProfileId = user2.Id,
                    Role = "member",
                    JoinedAt = conversation.CreatedAt
                };

                await context.Conversations.AddAsync(conversation);
                await context.UserConversations.AddRangeAsync(userConvo1, userConvo2);
                await context.SaveChangesAsync();

                // Add messages to conversation
                var numMessages = _random.Next(3, 15);
                for (int j = 0; j < numMessages; j++)
                {
                    var sender = j % 2 == 0 ? user1 : user2;
                    var messageContent = j switch
                    {
                        0 => "Hey! How are you?",
                        1 => "I'm good, thanks! Just wanted to say hi 👋",
                        2 => "That's awesome! Love your recent posts!",
                        _ => _commentContents[_random.Next(_commentContents.Length)]
                    };

                    var createdAt = conversation.CreatedAt.AddMinutes(j * _random.Next(5, 60));
                    var message = new Message
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        SenderId = sender.Id,
                        Content = messageContent,
                        CreatedAt = createdAt,
                        UpdatedAt = null,
                        IsEdited = false
                    };

                    await context.Messages.AddAsync(message);

                    // Add read receipt
                    var readBy = new MessageRead
                    {
                        MessageId = message.Id,
                        ProfileId = sender.Id == user1.Id ? user2.Id : user1.Id,
                        ReadAt = createdAt.AddMinutes(_random.Next(1, 10))
                    };
                    await context.MessageReads.AddAsync(readBy);

                    messageCount++;
                }

                conversationCount++;
            }

            Console.WriteLine($"[SeedData] Created {conversationCount} conversations and {messageCount} messages.");
        }

        private static async Task SeedNotificationsAsync(AppDbContext context, List<Profile> profiles, List<Post> posts, List<Comment> comments)
        {
            Console.WriteLine("[SeedData] Seeding Notifications...");
            var notifications = new List<Notification>();
            var notificationTypes = Enum.GetValues<NotificationType>();
            var now = DateTime.UtcNow;

            // Create 40 notifications
            for (int i = 0; i < 40; i++)
            {
                var recipient = profiles[i % profiles.Count];
                var actor = profiles[(i + 3) % profiles.Count];
                var notificationType = notificationTypes[_random.Next(notificationTypes.Length)];

                Guid? targetPostId = null;
                Guid? targetCommentId = null;
                string message = "";

                switch (notificationType)
                {
                    case NotificationType.Like:
                        var randomPost = posts[_random.Next(posts.Count)];
                        targetPostId = randomPost.Id;
                        message = $"liked your post";
                        break;
                    case NotificationType.Comment:
                        var commentPost = posts[_random.Next(posts.Count)];
                        targetPostId = commentPost.Id;
                        var randomComment = comments[_random.Next(comments.Count)];
                        targetCommentId = randomComment.Id;
                        message = $"commented on your post";
                        break;
                    case NotificationType.Follow:
                        message = "started following you";
                        break;
                    case NotificationType.Share:
                        var sharedPost = posts[_random.Next(posts.Count)];
                        targetPostId = sharedPost.Id;
                        message = $"shared your post";
                        break;
                    case NotificationType.System:
                        message = "Welcome to Favi! 🎉";
                        break;
                }

                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Type = notificationType,
                    RecipientProfileId = recipient.Id,
                    ActorProfileId = actor.Id,
                    TargetPostId = targetPostId,
                    TargetCommentId = targetCommentId,
                    Message = message,
                    IsRead = _random.Next(100) < 40, // 40% are read
                    CreatedAt = now.AddDays(-_random.Next(1, 30)).AddHours(-_random.Next(0, 24))
                };
                notifications.Add(notification);
            }

            await context.Notifications.AddRangeAsync(notifications);
            await context.SaveChangesAsync();
            Console.WriteLine($"[SeedData] Created {notifications.Count} notifications.");
        }
    }
}
