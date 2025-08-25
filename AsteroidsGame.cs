using System.Numerics;
using Raylib_cs;

namespace Space_Shooter
{
    internal class AsteroidsGame
    {
        // Constants
        private const int SCREEN_WIDTH = 800;
        private const int SCREEN_HEIGHT = 600;
        private const int INITIAL_ASTEROID_COUNT = 4;
        private const float PLAYER_RADIUS = 20.0f;

        // File paths
        private const string PLAYER_TEXTURE_PATH = @"C:\Tiedostot\Space Shooter\playerShip3_green.png";
        private const string ASTEROID_BROWN_PATH = @"C:\Tiedostot\Space Shooter\meteorBrown_big4.png";
        private const string ASTEROID_GREY_PATH = @"C:\Tiedostot\Space Shooter\meteorGrey_big4.png";
        private const string UFO_TEXTURE_PATH = @"C:\Tiedostot\Space Shooter\ufoYellow.png";
        private const string SHOOT_SOUND_PATH = @"C:\Tiedostot\Space Shooter\shooting-star-2-104073.mp3";
        private const string BACKGROUND_MUSIC_PATH = @"C:\Tiedostot\Space Shooter\space-sound-mid-109575.mp3";

        // Game objects
        private Ship player;
        private List<Asteroid> asteroids;
        private List<Enemy> enemies;
        private HighScoreManager highScoreManager;
        private Random random;

        // Game state
        private int score = 0;
        private int playerLives = 3; // إضافة الأرواح
        private bool gameRunning = true;
        private bool gameOver = false;
        private float gameTimer = 0f; // مؤقت اللعبة
        private bool enemySpawned = false; // هل ظهر العدو

        // Audio
        private Music backgroundMusic;
        private Sound shootSound;
        private bool audioLoaded = false;

        // Shared textures (loaded once)
        private static Texture2D playerTexture;
        private static Texture2D asteroidBrownTexture;
        private static Texture2D asteroidGreyTexture;
        private static Texture2D ufoTexture;
        private static bool texturesLoaded = false;

        public AsteroidsGame()
        {
            Raylib.InitWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Asteroids Game");
            Raylib.SetTargetFPS(60);

            random = new Random();
            highScoreManager = new HighScoreManager();

            LoadSharedResources();
            InitializeGame();
        }

        private void LoadSharedResources()
        {
            LoadTextures();
            InitializeAudio();
        }

        private void LoadTextures()
        {
            if (texturesLoaded) return;

            Console.WriteLine("Loading textures...");

            if (File.Exists(PLAYER_TEXTURE_PATH))
            {
                playerTexture = Raylib.LoadTexture(PLAYER_TEXTURE_PATH);
                Console.WriteLine("Player texture loaded successfully");
            }
            else
            {
                Console.WriteLine($"Player texture not found: {PLAYER_TEXTURE_PATH}");
            }

            if (File.Exists(ASTEROID_BROWN_PATH))
            {
                asteroidBrownTexture = Raylib.LoadTexture(ASTEROID_BROWN_PATH);
                Console.WriteLine("Brown asteroid texture loaded successfully");
            }
            else
            {
                Console.WriteLine($"Brown asteroid texture not found: {ASTEROID_BROWN_PATH}");
            }

            if (File.Exists(ASTEROID_GREY_PATH))
            {
                asteroidGreyTexture = Raylib.LoadTexture(ASTEROID_GREY_PATH);
                Console.WriteLine("Grey asteroid texture loaded successfully");
            }
            else
            {
                Console.WriteLine($"Grey asteroid texture not found: {ASTEROID_GREY_PATH}");
            }

            if (File.Exists(UFO_TEXTURE_PATH))
            {
                ufoTexture = Raylib.LoadTexture(UFO_TEXTURE_PATH);
                Console.WriteLine("UFO texture loaded successfully");
            }
            else
            {
                Console.WriteLine($"UFO texture not found: {UFO_TEXTURE_PATH}");
            }

            texturesLoaded = true;
        }

        private void InitializeAudio()
        {
            try
            {
                Raylib.InitAudioDevice();
                Console.WriteLine("Audio device initialized");

                if (File.Exists(BACKGROUND_MUSIC_PATH))
                {
                    backgroundMusic = Raylib.LoadMusicStream(BACKGROUND_MUSIC_PATH);
                    if (Raylib.IsMusicValid(backgroundMusic))
                    {
                        Raylib.PlayMusicStream(backgroundMusic);
                        Console.WriteLine("Background music loaded and playing");
                    }
                }
                else
                {
                    Console.WriteLine($"Background music not found: {BACKGROUND_MUSIC_PATH}");
                }

                if (File.Exists(SHOOT_SOUND_PATH))
                {
                    shootSound = Raylib.LoadSound(SHOOT_SOUND_PATH);
                    if (Raylib.IsSoundValid(shootSound))
                    {
                        Console.WriteLine("Shoot sound loaded successfully");
                    }
                }
                else
                {
                    Console.WriteLine($"Shoot sound not found: {SHOOT_SOUND_PATH}");
                }

                audioLoaded = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading audio: {e.Message}");
                audioLoaded = false;
            }
        }

        private void InitializeGame()
        {
            score = 0;
            playerLives = 3; // 
            gameOver = false;
            gameRunning = true;
            gameTimer = 0f;
            enemySpawned = false;

            // Create player
            player = new Ship(new Vector2(SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2));

            // Create asteroids
            asteroids = new List<Asteroid>();
            CreateInitialAsteroids();


            enemies = new List<Enemy>();
        }

        private void CreateInitialAsteroids()
        {
            asteroids.Clear();
            for (int i = 0; i < INITIAL_ASTEROID_COUNT; i++)
            {
                CreateAsteroid(2);
            }
        }

        public void Run()
        {
            while (!Raylib.WindowShouldClose() && gameRunning)
            {
                float deltaTime = Raylib.GetFrameTime();
                Update(deltaTime);
                Draw();
            }

            CleanupResources();
        }

        private void Update(float deltaTime)
        {
            // Update music
            if (audioLoaded && Raylib.IsMusicValid(backgroundMusic))
                Raylib.UpdateMusicStream(backgroundMusic);

            // Handle high score manager updates
            if (highScoreManager.CurrentState == HighScoreManager.State.EnteringName)
            {
                highScoreManager.UpdateNameEntry();
                return;
            }
            else if (highScoreManager.CurrentState == HighScoreManager.State.ShowingScores)
            {
                highScoreManager.UpdateShowingScores();
                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    InitializeGame(); // Restart game
                }
                else if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    gameRunning = false; // Exit game
                }
                return;
            }

            if (gameOver) return;


            gameTimer += deltaTime;

            // Update player
            player.Update(deltaTime);

            // Handle shooting sound
            if (Raylib.IsKeyPressed(KeyboardKey.Space) && audioLoaded && Raylib.IsSoundValid(shootSound))
            {
                Raylib.PlaySound(shootSound);
            }

            // Update asteroids
            UpdateAsteroids(deltaTime);

            // Update enemies
            UpdateEnemies(deltaTime);

            // Check collisions
            CheckBulletAsteroidCollisions();
            CheckBulletEnemyCollisions();
            CheckPlayerAsteroidCollisions();
            CheckPlayerEnemyCollisions();
            CheckEnemyBulletPlayerCollisions();

            // Check if all asteroids are destroyed
            if (asteroids.Count == 0)
            {
                CreateInitialAsteroids();
            }
        }

        private void UpdateAsteroids(float deltaTime)
        {
            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                asteroids[i].Update(deltaTime);

                if (!asteroids[i].IsActive)
                {
                    asteroids.RemoveAt(i);
                }
            }
        }

        private void CheckPlayerAsteroidCollisions()
        {
            Vector2 playerPos = player.GetPosition();

            foreach (var asteroid in asteroids)
            {
                if (Raylib.CheckCollisionCircles(playerPos, PLAYER_RADIUS, asteroid.GetPosition(), asteroid.GetRadius()))
                {
                    PlayerHit();
                    return;
                }
            }
        }

        private void GameOver()
        {
            gameOver = true;
            Console.WriteLine($"Game Over! Final Score: {score}");

            if (highScoreManager.IsHighScore(score))
            {
                highScoreManager.StartNameEntry(score);
            }
            else
            {
                // Wait a moment, then restart
                System.Threading.Thread.Sleep(1000);
                InitializeGame();
            }
        }

        private void Draw()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            if (highScoreManager.CurrentState == HighScoreManager.State.EnteringName)
            {
                highScoreManager.DrawNameEntry(SCREEN_WIDTH, SCREEN_HEIGHT);
            }
            else if (highScoreManager.CurrentState == HighScoreManager.State.ShowingScores)
            {
                highScoreManager.DrawHighScores(SCREEN_WIDTH, SCREEN_HEIGHT);
            }
            else
            {
                // Draw normal game
                player.Draw();

                foreach (var asteroid in asteroids)
                {
                    asteroid.Draw();
                }

                foreach (var enemy in enemies)
                {
                    enemy.Draw();
                }

                // Draw UI
                Raylib.DrawText($"Score: {score}", 10, 10, 20, Color.White);

                // رسم الأرواح
                Raylib.DrawText("Lives:", 10, 40, 20, Color.White);
                for (int i = 0; i < playerLives; i++)
                {
                    Raylib.DrawRectangle(80 + i * 30, 40, 20, 20, Color.Red);
                }

                Raylib.DrawText("Controls: Arrows/WASD to move, Space to shoot", 10, SCREEN_HEIGHT - 30, 20, Color.White);

                if (gameOver)
                {
                    string gameOverText = "GAME OVER";
                    int textWidth = Raylib.MeasureText(gameOverText, 40);
                    Raylib.DrawText(gameOverText, SCREEN_WIDTH / 2 - textWidth / 2, SCREEN_HEIGHT / 2 - 20, 40, Color.Red);
                }
            }

            Raylib.EndDrawing();
        }

        private void CreateAsteroid(int size)
        {
            Vector2 position = GetRandomSpawnPosition();
            Vector2 direction = GetDirectionToCenter(position);
            float speed = 50 + (float)random.NextDouble() * 50;
            Vector2 velocity = direction * speed;
            float rotationSpeed = (float)(random.NextDouble() * 200 - 100);

            asteroids.Add(new Asteroid(position, velocity, size, rotationSpeed));
        }

        private Vector2 GetRandomSpawnPosition()
        {
            int side = random.Next(4);
            return side switch
            {
                0 => new Vector2(random.Next(SCREEN_WIDTH), -50), // Top
                1 => new Vector2(SCREEN_WIDTH + 50, random.Next(SCREEN_HEIGHT)), // Right
                2 => new Vector2(random.Next(SCREEN_WIDTH), SCREEN_HEIGHT + 50), // Bottom
                _ => new Vector2(-50, random.Next(SCREEN_HEIGHT)) // Left
            };
        }

        private Vector2 GetDirectionToCenter(Vector2 fromPosition)
        {
            Vector2 direction = new Vector2(SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2) - fromPosition;
            return Vector2.Normalize(direction);
        }

        private void SplitAsteroid(Asteroid asteroid)
        {
            score += asteroid.GetSize() * 100;


            if (asteroid.GetSize() == 2)
            {

                for (int i = 0; i < 2; i++)
                {
                    float angle = (float)random.NextDouble() * MathF.PI * 2;
                    Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    Vector2 velocity = direction * (70 + (float)random.NextDouble() * 30);
                    float rotationSpeed = (float)(random.NextDouble() * 200 - 100);

                    asteroids.Add(new Asteroid(asteroid.GetPosition(), velocity, 1, rotationSpeed));
                }
            }


            asteroid.Destroy();
        }

        private void CreateEnemy()
        {
            Vector2 position = GetRandomSpawnPosition();
            enemies.Add(new Enemy(position));
        }

        private void UpdateEnemies(float deltaTime)
        {

            if (!enemySpawned && gameTimer >= 15.0f)
            {
                CreateEnemy();
                enemySpawned = true;
                Console.WriteLine("Enemy spawned after 15 seconds!");
            }

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                enemies[i].Update(deltaTime, player.GetPosition());

                if (!enemies[i].IsActive)
                {
                    enemies.RemoveAt(i);
                }
            }


            if (enemySpawned && enemies.Count == 0 && random.Next(600) < 1)
            {
                CreateEnemy();
            }
        }

        private void CheckBulletEnemyCollisions()
        {
            List<Bullet> bullets = player.GetBullets();

            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    if (Raylib.CheckCollisionCircles(bullets[i].GetPosition(), 3, enemies[j].GetPosition(), enemies[j].GetRadius()))
                    {
                        score += 500;
                        enemies[j].Destroy();
                        bullets[i].Destroy();
                        break;
                    }
                }
            }
        }

        private void CheckPlayerEnemyCollisions()
        {
            Vector2 playerPos = player.GetPosition();

            foreach (var enemy in enemies)
            {
                if (Raylib.CheckCollisionCircles(playerPos, PLAYER_RADIUS, enemy.GetPosition(), enemy.GetRadius()))
                {
                    PlayerHit();
                    return;
                }
            }
        }

        private void CheckEnemyBulletPlayerCollisions()
        {
            Vector2 playerPos = player.GetPosition();

            foreach (var enemy in enemies)
            {
                var enemyBullets = enemy.GetBullets();
                for (int i = enemyBullets.Count - 1; i >= 0; i--)
                {
                    if (Raylib.CheckCollisionCircles(playerPos, PLAYER_RADIUS, enemyBullets[i].GetPosition(), 3))
                    {
                        enemyBullets[i].Destroy(); // تدمير الرصاصة
                        PlayerHit(); // استدعاء دالة إصابة اللاعب
                        return;
                    }
                }
            }
        }


        private void PlayerHit()
        {
            playerLives--;


            player = new Ship(new Vector2(SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2));

            Console.WriteLine($"Player hit! Lives remaining: {playerLives}");


            if (playerLives <= 0)
            {
                GameOver();
            }
        }

        private void CheckBulletAsteroidCollisions()
        {
            List<Bullet> bullets = player.GetBullets();

            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                for (int j = asteroids.Count - 1; j >= 0; j--)
                {
                    if (Raylib.CheckCollisionCircles(bullets[i].GetPosition(), 3, asteroids[j].GetPosition(), asteroids[j].GetRadius()))
                    {
                        SplitAsteroid(asteroids[j]);
                        bullets[i].Destroy();
                        break;
                    }
                }
            }
        }

        private void CleanupResources()
        {
            Console.WriteLine("Cleaning up resources...");

            // Free audio resources
            if (audioLoaded)
            {
                if (Raylib.IsMusicValid(backgroundMusic))
                    Raylib.UnloadMusicStream(backgroundMusic);

                if (Raylib.IsSoundValid(shootSound))
                    Raylib.UnloadSound(shootSound);

                Raylib.CloseAudioDevice();
            }

            // Free textures
            if (texturesLoaded)
            {
                if (playerTexture.Id != 0) Raylib.UnloadTexture(playerTexture);
                if (asteroidBrownTexture.Id != 0) Raylib.UnloadTexture(asteroidBrownTexture);
                if (asteroidGreyTexture.Id != 0) Raylib.UnloadTexture(asteroidGreyTexture);
                if (ufoTexture.Id != 0) Raylib.UnloadTexture(ufoTexture);
            }

            Raylib.CloseWindow();
        }

        // Static methods to access shared resources
        public static Texture2D GetPlayerTexture() => playerTexture;

        public static Texture2D GetAsteroidTexture(int size)
        {

            Random rand = new Random();
            if (rand.Next(2) == 0 && asteroidBrownTexture.Id != 0)
                return asteroidBrownTexture;
            else if (asteroidGreyTexture.Id != 0)
                return asteroidGreyTexture;
            else if (asteroidBrownTexture.Id != 0)
                return asteroidBrownTexture;
            else
                return new Texture2D(); // Empty texture
        }

        public static Texture2D GetUfoTexture() => ufoTexture;

        public static bool AreTexturesLoaded() => texturesLoaded;
        public static float GetPlayerRadius() => PLAYER_RADIUS;
        public static int GetScreenWidth() => SCREEN_WIDTH;
        public static int GetScreenHeight() => SCREEN_HEIGHT;
    }
}