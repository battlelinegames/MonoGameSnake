using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Diagnostics;

namespace Snake;

public class SnakeGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _snakeHead;
    private Texture2D _snakeBody;
    private Texture2D _foodSprite;
    private List<Vector2> _snake;
    private Vector2 _food;
    private Vector2 _direction;
    private float _timer;
    private const float _moveInterval = 0.1f;
    private int _score;
    private SpriteFont _font;
    private bool _gameOver;
    private bool _fontLoaded;
    private const int SEGMENT_SIZE = 20;
    
    // Game state management
    private enum GameState
    {
        StartMenu,
        Playing,
        GameOver
    }
    private GameState _currentState;
    private KeyboardState _previousKeyboardState;
    private string[] _menuOptions = { "Start Game", "Exit" };
    private int _selectedMenuOption = 0;

    public SnakeGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
    }

    protected override void Initialize()
    {
        Debug.WriteLine("SnakeGame.Initialize() called");
        _currentState = GameState.StartMenu;
        InitializeGame();
        base.Initialize();
    }

    private void InitializeGame()
    {
        _snake = new List<Vector2>();
        _snake.Add(new Vector2(400, 300)); // Starting position
        _direction = new Vector2(SEGMENT_SIZE, 0); // Start moving right
        _score = 0;
        _gameOver = false;
        //_fontLoaded = false;
        SpawnFood();
        Debug.WriteLine($"Initialized snake at position: {_snake[0]}, Food at: {_food}");
    }

    protected override void LoadContent()
    {
        Debug.WriteLine("SnakeGame.LoadContent() called");
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        try
        {
            _font = Content.Load<SpriteFont>("Font");
            _fontLoaded = true;
            Debug.WriteLine("Font loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load font: {ex.Message}");
            _fontLoaded = false;
        }

        try
        {
            Debug.WriteLine("Attempting to load snake_head texture...");
            _snakeHead = Content.Load<Texture2D>("snake_head");
            Debug.WriteLine($"Snake head texture loaded successfully. Size: {_snakeHead.Width}x{_snakeHead.Height}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load snake head texture: {ex.Message}");
            Debug.WriteLine($"Exception details: {ex}");
            // Create a placeholder texture if loading fails
            _snakeHead = new Texture2D(GraphicsDevice, SEGMENT_SIZE, SEGMENT_SIZE);
            Color[] data = new Color[SEGMENT_SIZE * SEGMENT_SIZE];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Color.Yellow;
            }
            _snakeHead.SetData(data);
            Debug.WriteLine("Created yellow placeholder texture");
        }

        try
        {
            Debug.WriteLine("Attempting to load snake_body texture...");
            _snakeBody = Content.Load<Texture2D>("snake_body");
            Debug.WriteLine($"Snake body texture loaded successfully. Size: {_snakeBody.Width}x{_snakeBody.Height}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load snake body texture: {ex.Message}");
            // Create a placeholder texture if loading fails
            _snakeBody = new Texture2D(GraphicsDevice, SEGMENT_SIZE, SEGMENT_SIZE);
            Color[] data = new Color[SEGMENT_SIZE * SEGMENT_SIZE];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Color.Green;
            }
            _snakeBody.SetData(data);
            Debug.WriteLine("Created green placeholder texture");
        }

        try
        {
            Debug.WriteLine("Attempting to load food texture...");
            _foodSprite = Content.Load<Texture2D>("food");
            Debug.WriteLine($"Food texture loaded successfully. Size: {_foodSprite.Width}x{_foodSprite.Height}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load food texture: {ex.Message}");
            // Create a placeholder texture if loading fails
            _foodSprite = new Texture2D(GraphicsDevice, SEGMENT_SIZE, SEGMENT_SIZE);
            Color[] data = new Color[SEGMENT_SIZE * SEGMENT_SIZE];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Color.Red;
            }
            _foodSprite.SetData(data);
            Debug.WriteLine("Created red placeholder texture");
        }

        Debug.WriteLine("Content loaded successfully");
    }

    private void SpawnFood()
    {
        Debug.WriteLine("Spawning new food");
        Random random = new Random();
        int x = random.Next(0, _graphics.PreferredBackBufferWidth / SEGMENT_SIZE) * SEGMENT_SIZE;
        int y = random.Next(0, _graphics.PreferredBackBufferHeight / SEGMENT_SIZE) * SEGMENT_SIZE;
        _food = new Vector2(x, y);
        Debug.WriteLine($"New food position: {_food}");
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentKeyboardState = Keyboard.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
            currentKeyboardState.IsKeyDown(Keys.Escape))
        {
            Debug.WriteLine("Exit requested");
            Exit();
        }

        switch (_currentState)
        {
            case GameState.StartMenu:
                UpdateStartMenu(currentKeyboardState);
                break;

            case GameState.Playing:
                UpdateGame(gameTime, currentKeyboardState);
                break;

            case GameState.GameOver:
                //UpdateGameOver(currentKeyboardState);
                UpdateStartMenu(currentKeyboardState);
                break;
        }

        _previousKeyboardState = currentKeyboardState;
        base.Update(gameTime);
    }

    private void UpdateStartMenu(KeyboardState currentKeyboardState)
    {
        // Handle menu navigation
        if (currentKeyboardState.IsKeyDown(Keys.Down) && 
            _previousKeyboardState.IsKeyUp(Keys.Down))
        {
            _selectedMenuOption = (_selectedMenuOption + 1) % _menuOptions.Length;
        }
        if (currentKeyboardState.IsKeyDown(Keys.Up) && 
            _previousKeyboardState.IsKeyUp(Keys.Up))
        {
            _selectedMenuOption = (_selectedMenuOption - 1 + _menuOptions.Length) % _menuOptions.Length;
        }

        // Handle menu selection
        if (currentKeyboardState.IsKeyDown(Keys.Enter) && 
            _previousKeyboardState.IsKeyUp(Keys.Enter))
        {
            if (_selectedMenuOption == 0) // Start Game
            {
                _currentState = GameState.Playing;
                InitializeGame();
            }
            else if (_selectedMenuOption == 1) // Exit
            {
                Exit();
            }
        }
    }

    private void UpdateGame(GameTime gameTime, KeyboardState keyboardState)
    {
        if (!_gameOver)
        {
            // Handle input
            if (keyboardState.IsKeyDown(Keys.Up) && _direction.Y != SEGMENT_SIZE)
            {
                _direction = new Vector2(0, -SEGMENT_SIZE);
                Debug.WriteLine("Direction changed: Up");
            }
            if (keyboardState.IsKeyDown(Keys.Down) && _direction.Y != -SEGMENT_SIZE)
            {
                _direction = new Vector2(0, SEGMENT_SIZE);
                Debug.WriteLine("Direction changed: Down");
            }
            if (keyboardState.IsKeyDown(Keys.Left) && _direction.X != SEGMENT_SIZE)
            {
                _direction = new Vector2(-SEGMENT_SIZE, 0);
                Debug.WriteLine("Direction changed: Left");
            }
            if (keyboardState.IsKeyDown(Keys.Right) && _direction.X != -SEGMENT_SIZE)
            {
                _direction = new Vector2(SEGMENT_SIZE, 0);
                Debug.WriteLine("Direction changed: Right");
            }

            // Move snake
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer >= _moveInterval)
            {
                _timer = 0;
                Vector2 newHead = _snake[0] + _direction;
                Debug.WriteLine($"Moving snake to new head position: {newHead}");

                // Check for collisions with walls
                if (newHead.X < 0 || newHead.X >= _graphics.PreferredBackBufferWidth ||
                    newHead.Y < 0 || newHead.Y >= _graphics.PreferredBackBufferHeight)
                {
                    Debug.WriteLine("Game Over: Wall collision");
                    _currentState = GameState.GameOver;
                    return;
                }

                // Check for self-collision
                for (int i = 1; i < _snake.Count; i++)
                {
                    if (newHead == _snake[i])
                    {
                        Debug.WriteLine("Game Over: Self collision");
                        _currentState = GameState.GameOver;
                        return;
                    }
                }

                _snake.Insert(0, newHead);

                // Check for food collision
                if (newHead == _food)
                {
                    Debug.WriteLine("Food eaten!");
                    _score += 10;
                    SpawnFood();
                }
                else
                {
                    _snake.RemoveAt(_snake.Count - 1);
                }
            }
        }
    }

    private void UpdateGameOver(KeyboardState currentKeyboardState)
    {
        // Handle menu navigation just like in start menu
        if (currentKeyboardState.IsKeyDown(Keys.Down) && 
            _previousKeyboardState.IsKeyUp(Keys.Down))
        {
            _selectedMenuOption = (_selectedMenuOption + 1) % _menuOptions.Length;
        }
        if (currentKeyboardState.IsKeyDown(Keys.Up) && 
            _previousKeyboardState.IsKeyUp(Keys.Up))
        {
            _selectedMenuOption = (_selectedMenuOption - 1 + _menuOptions.Length) % _menuOptions.Length;
        }

        // Handle menu selection
        if (currentKeyboardState.IsKeyDown(Keys.Enter) && 
            _previousKeyboardState.IsKeyUp(Keys.Enter))
        {
            if (_selectedMenuOption == 0) // Start Game
            {
                _currentState = GameState.Playing;
                InitializeGame();
            }
            else if (_selectedMenuOption == 1) // Exit
            {
                Exit();
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin();

        switch (_currentState)
        {
            case GameState.StartMenu:
                DrawStartMenu();
                break;

            case GameState.Playing:
                DrawGame();
                break;

            case GameState.GameOver:
                //DrawGameOver();
                DrawStartMenu();
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawStartMenu()
    {
        if (!_fontLoaded) {
            Debug.WriteLine("font not loaded");
            return;
        }

        string title = _currentState == GameState.GameOver ? "GAME OVER" : "SNAKE GAME";
        Vector2 titleSize = _font.MeasureString(title);
        float startY = _graphics.PreferredBackBufferHeight / 3;

        // Draw title
        Color titleColor = _currentState == GameState.GameOver ? Color.Red : Color.Green;
        _spriteBatch.DrawString(_font, title,
            new Vector2((_graphics.PreferredBackBufferWidth - titleSize.X) / 2, startY),
            titleColor);

        // Draw score if game over
        if (_currentState == GameState.GameOver)
        {
            string scoreText = $"Final Score: {_score}";
            Vector2 scoreSize = _font.MeasureString(scoreText);
            _spriteBatch.DrawString(_font, scoreText,
                new Vector2((_graphics.PreferredBackBufferWidth - scoreSize.X) / 2, startY + 50),
                Color.White);
        }

        // Draw menu options
        float menuStartY = startY + (_currentState == GameState.GameOver ? 120 : 100);
        for (int i = 0; i < _menuOptions.Length; i++)
        {
            Vector2 textSize = _font.MeasureString(_menuOptions[i]);
            Vector2 position = new Vector2(
                (_graphics.PreferredBackBufferWidth - textSize.X) / 2,
                menuStartY + (i * 50));
            
            Color color = i == _selectedMenuOption ? Color.Yellow : Color.White;
            _spriteBatch.DrawString(_font, _menuOptions[i], position, color);
        }
    }

    private void DrawGameOver()
    {
        // Draw snake body only (no head)
        for (int i = 1; i < _snake.Count; i++)
        {
            _spriteBatch.Draw(_snakeBody, 
                new Rectangle((int)_snake[i].X, (int)_snake[i].Y, SEGMENT_SIZE, SEGMENT_SIZE), 
                Color.White);
        }

        // Draw food
        _spriteBatch.Draw(_foodSprite, 
            new Rectangle((int)_food.X, (int)_food.Y, SEGMENT_SIZE, SEGMENT_SIZE), 
            Color.White);

        // Draw score if font is loaded
        if (_fontLoaded)
        {
            _spriteBatch.DrawString(_font, $"Score: {_score}", new Vector2(10, 10), Color.White);
        }
    }

    private void DrawGame()
    {
        // Draw snake body
        for (int i = 0; i < _snake.Count; i++)
        {
            _spriteBatch.Draw(_snakeBody, 
                new Rectangle((int)_snake[i].X, (int)_snake[i].Y, SEGMENT_SIZE, SEGMENT_SIZE), 
                Color.White);
        }

        // Draw snake head with rotation based on direction
        float rotation = 0f;
        if (_direction.X < 0) rotation = MathHelper.Pi; // Left
        else if (_direction.Y < 0) rotation = -MathHelper.PiOver2; // Up
        else if (_direction.Y > 0) rotation = MathHelper.PiOver2; // Down

        // Get the head position from the snake list
        Vector2 headPos = _snake[0];

        // Draw food
        _spriteBatch.Draw(_foodSprite, 
            new Rectangle((int)_food.X, (int)_food.Y, SEGMENT_SIZE, SEGMENT_SIZE), 
            Color.White);

        // Draw score if font is loaded
        if (_fontLoaded)
        {
            _spriteBatch.DrawString(_font, $"Score: {_score}", new Vector2(10, 10), Color.White);
        }
    }
}
