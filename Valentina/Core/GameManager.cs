using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MonoGame.Extended.Input;
using Stateless;
using Valentina.Core.Data;

namespace Valentina.Core;

public class GameManager : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    private Texture2D _head, _body, _jaw;
    private const int WIDTH = 800, HEIGHT = 800;
    
    private MouseState _currentMouse;
    private MouseState _previousMouse;
    private StateMachine<DollStates, DollTriggers> _stateMachine;
    
    private float _stateTimer = 0f, _cooldownTimer = 0f;
    private int _jawPos;
    private float _jawTimer = 0f;
    private string _currentPhrase = "";
    private bool _wasPaused = false;
    
    private readonly Random _random = new();
    private PhraseManager _phraseManager;
    
    private List<Song> _musicTracks = new();
    private int _currentTrackIndex = 0;

    public GameManager()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = WIDTH;
        _graphics.PreferredBackBufferHeight = HEIGHT;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        _stateMachine = new StateMachine<DollStates, DollTriggers>(DollStates.Idle);
        ConfigureStateMachine();
        base.Initialize();
    }

    private SpriteFont _font;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _body = Content.Load<Texture2D>("Textures/Doll/body");
        _head = Content.Load<Texture2D>("Textures/Doll/head");
        _jaw = Content.Load<Texture2D>("Textures/Doll/jaw");
        _font = Content.Load<SpriteFont>("Default");
        
        string[] trackNames = { "Musics/Song1", "Musics/Song2" };
        foreach (var name in trackNames)
        {
            try { _musicTracks.Add(Content.Load<Song>(name)); }
            catch
            {
                // ignored
            }
        }

        PlayRandomMusic();
        _phraseManager = new PhraseManager();
        _phraseManager.Start();
    }
    
    private void PlayRandomMusic()
    {
        if (_musicTracks.Count == 0) return;
        
        _currentTrackIndex = (_currentTrackIndex + 1) % _musicTracks.Count;
    
        MediaPlayer.IsRepeating = false;
        MediaPlayer.Volume = 0.3f;
        MediaPlayer.Play(_musicTracks[_currentTrackIndex]);
    }

    private void ConfigureStateMachine()
    {
        _stateMachine.Configure(DollStates.Idle)
            .Permit(DollTriggers.StartSpeaking, DollStates.Speaking);
        
        _stateMachine.Configure(DollStates.Speaking)
            .Permit(DollTriggers.Interrupt, DollStates.Paused)
            .Permit(DollTriggers.MakeAngry, DollStates.Angry)
            .Permit(DollTriggers.EndSpeaking, DollStates.Idle);
        
        _stateMachine.Configure(DollStates.Paused)
            .Permit(DollTriggers.Resume, DollStates.Speaking)
            .Permit(DollTriggers.MakeAngry, DollStates.Angry);
        
        _stateMachine.Configure(DollStates.Angry)
            .Permit(DollTriggers.EndSpeaking, DollStates.Recovery)
            .Permit(DollTriggers.MakeOffended, DollStates.Offended);
        
        _stateMachine.Configure(DollStates.Recovery) 
            .Permit(DollTriggers.MakeOffended, DollStates.Offended);
            
        _stateMachine.Configure(DollStates.Offended)
            .Permit(DollTriggers.Reset, DollStates.Idle);
    }

    protected override void Update(GameTime gameTime)
    {
        _previousMouse = _currentMouse;
        _currentMouse = Mouse.GetState();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
    
        _stateTimer += deltaTime;
        if (_cooldownTimer > 0) _cooldownTimer -= deltaTime;
        
        if (_currentMouse.LeftButton == ButtonState.Pressed && 
            _previousMouse.LeftButton == ButtonState.Released && 
            _cooldownTimer <= 0)
        {
            HandleMouseClick();
        }
    
        UpdateStateLogic(deltaTime);
        UpdateJawAnimation(deltaTime);
    
        base.Update(gameTime);
    }

    private void HandleMouseClick()
    {
        switch (_stateMachine.State)
        {
            case DollStates.Idle:
                _stateMachine.Fire(DollTriggers.StartSpeaking);
                _currentPhrase = GetRandomPhrase(_phraseManager.Config.IntroPhrases.ToArray());
                _wasPaused = false;
                break;
                
            case DollStates.Speaking:
                if (!_wasPaused)
                {
                    _stateMachine.Fire(DollTriggers.Interrupt);
                    _currentPhrase = "";
                }
                else
                {
                    _stateMachine.Fire(DollTriggers.MakeAngry);
                    _currentPhrase = GetRandomPhrase(_phraseManager.Config.AngryPhrases.ToArray());
                }
                break;
                
            case DollStates.Paused:
                _stateMachine.Fire(DollTriggers.MakeAngry);
                _currentPhrase = GetRandomPhrase(_phraseManager.Config.AngryPhrases.ToArray());
                break;
                
            case DollStates.Angry:
            case DollStates.Recovery:
                _stateMachine.Fire(DollTriggers.MakeOffended);
                _cooldownTimer = 10f;
                _currentPhrase = GetRandomPhrase(_phraseManager.Config.OffendedPhrases.ToArray());
                break;
        }
    }

    private void UpdateStateLogic(float deltaTime)
    {
        switch (_stateMachine.State)
        {
            case DollStates.Speaking:
                if (_stateTimer > 6f)
                {
                    _stateMachine.Fire(DollTriggers.EndSpeaking);
                    _currentPhrase = "";
                    _stateTimer = 0f;
                }
                else if (string.IsNullOrEmpty(_currentPhrase) && _stateTimer > 0.5f)
                {
                    _currentPhrase = GetRandomPhrase(_phraseManager.Config.PredictionPhrases.ToArray());
                }
                break;
                
            case DollStates.Paused:
                _jawPos = 0;
                if (_stateTimer >= 0.5f)
                {
                    _stateMachine.Fire(DollTriggers.Resume);
                    _wasPaused = true;
                    _currentPhrase = GetRandomPhrase(_phraseManager.Config.PredictionPhrases.ToArray());
                    _stateTimer = 0f;
                }
                break;
                
            case DollStates.Angry:
                if (_stateTimer >= 3f)
                {
                    _stateMachine.Fire(DollTriggers.EndSpeaking);
                    _currentPhrase = "";
                    _stateTimer = 0f;
                }
                break;
                
            case DollStates.Recovery:
                _jawPos = 0;
                if (_stateTimer >= 0.7f)
                {
                    _stateMachine.Fire(DollTriggers.MakeOffended);
                    _currentPhrase = GetRandomPhrase(_phraseManager.Config.OffendedPhrases.ToArray());
                    _stateTimer = 0f;
                }
                break;
                
            case DollStates.Offended:
                _jawPos = 0;
                if (_stateTimer >= 10f)
                {
                    _stateMachine.Fire(DollTriggers.Reset);
                    _currentPhrase = "";
                    _stateTimer = 0f;
                }
                break;
                
            case DollStates.Idle:
                _jawPos = 0;
                _stateTimer = 0f;
                break;
        }
    }

    private void UpdateJawAnimation(float deltaTime)
    {
        bool isSpeaking = _stateMachine.State == DollStates.Speaking || _stateMachine.State == DollStates.Angry;
        
        if (isSpeaking && !string.IsNullOrEmpty(_currentPhrase))
        {
            _jawTimer += deltaTime;
            float interval = _stateMachine.State == DollStates.Angry ? 0.1f : 0.2f;
            if (_jawTimer >= interval)
            {
                _jawPos = (_jawPos == 0) ? 10 : 0;
                _jawTimer = 0f;
            }
        }
        else _jawPos = 0;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _spriteBatch.Draw(_body, new Rectangle(0, 0, WIDTH, HEIGHT), Color.White);
        _spriteBatch.Draw(_jaw, new Rectangle(0, _jawPos, WIDTH, HEIGHT), Color.White);
        _spriteBatch.Draw(_head, new Rectangle(0, 0, WIDTH, HEIGHT), Color.White);
        
        if (!string.IsNullOrEmpty(_currentPhrase))
            DrawText(_currentPhrase, new Vector2(WIDTH / 2, HEIGHT - 50));
        
        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawText(string text, Vector2 position)
    {
        var size = _font.MeasureString(text);
        _spriteBatch.DrawString(_font, text, position - size / 2, Color.White);
    }
    
    private string GetRandomPhrase(string[] phrases)
        => phrases?.Length > 0 ? phrases[_random.Next(phrases.Length)] : "";
}


public enum DollStates 
{
    Idle, Speaking, Paused, Angry, Recovery, Offended
}

public enum DollTriggers
{
    StartSpeaking, Interrupt, Resume, EndSpeaking, MakeAngry, MakeOffended, Reset
}