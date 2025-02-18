/**** 
 * Created by: Akram Taghavi-Burrs
 * Date Created: Feb 23, 2022
 * 
 * Last Edited by: Andrew Nguyen
 * Last Edited: Mar 12, 2022
 * 
 * Description: GameManager for Mole Disposal game
****/

/** Import Libraries **/
using System; //C# library for system properties
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; //libraries for accessing scenes


public class GameManager : MonoBehaviour
{
    /*** VARIABLES ***/

    #region GameManager Singleton
    static private GameManager gm; //refence GameManager
    static public GameManager GM { get { return gm; } } //public access to read only gm 

    //Check to make sure only one gm of the GameManager is in the scene
    void CheckGameManagerIsInScene()
    {

        //Check if instnace is null
        if (gm == null)
        {
            gm = this; //set gm to this gm of the game object
            Debug.Log(gm);
        }
        else //else if gm is not null a Game Manager must already exsist
        {
            Destroy(this.gameObject); //In this case you need to delete this gm
        }
        DontDestroyOnLoad(this); //Do not delete the GameManager when scenes load
        Debug.Log(gm);
    }//end CheckGameManagerIsInScene()
    #endregion

    [Header("GENERAL SETTINGS")]
    public string gameTitle = "Mole Disposal";  //name of the game
    public string gameCredit = "Made by: Andrew Nguyen"; //Game creator
    public string gameHelpText = "Help Vivian beat back the moles! Use arrow keys or WASD to move her around and the left mouse button to attack when next to a mole. You must attack in the order displayed at the beginning of the level - but good thing you get three tries! The sequence gets harder as levels goes on.";
    public string helpTitle = "How to Play";
    public string copyrightYear = "Copyright � " + thisDay; //date cretaed
    public GameObject mole;
    public float time = 35.0f; //Time. This is set dynamically at least on initial concept doc, but that feature may be cut. By default it's 30
    public List<GameObject> moles; 
    public List<GameObject> playerHits; //The player's hits

    //Test HighScore ONLY
    public static int testHS = 1500;

    [Header("GAME SETTINGS")]

    public bool sequencing = false; //This is so the player can't move when the moles are beeping the sequence. By default should be false.
    //static vairables can not be updated in the inspector, however private serialized fileds can be
    [SerializeField] //Access to private variables in editor
    private int numberOfLives; //set number of lives in the inspector
    static public int lives; // number of lives for player 
    public int Lives { get { return lives; } set { lives = value; } }//access to private variable lives [get/set methods]

    static public int score; //score value. Calculated with the time remaining and how many moles were there. More moles means more points.
    public int Score { get { return score; } set { score = value; } } //access to private variable score [get/set methods]

    [Space(10)]

    [SerializeField] //Access to private variables in editor
    static public int highScore; //Default high score
    public int HighScore { get { return highScore; } set { highScore = value; } } //access to private variable highScore [get/set methods]

    [Space(10)]

    [Tooltip("Check to test player lost the level")]
    private bool levelLost = false;//we have lost the level (ie. player died)
    public bool LevelLost { get { return levelLost; } set { levelLost = value; } } //access to private variable lostLevel [get/set methods]

    [Space(10)]
    public string defaultEndMessage = "Game Over";//the end screen message, depends on winning outcome
    public string looseMessage = "Oh no..."; //Message if player looses
    public string winMessage = "Right on!"; //Message if player wins
    [HideInInspector] public string endMsg;//the end screen message, depends on winning outcome

    [Header("SCENE SETTINGS")]
    [Tooltip("Name of the start scene")]
    public string startScene;

    [Tooltip("Name of the game over scene")]
    public string gameOverScene;

    [Tooltip("Name of the Help Scene")]
    public string helpScene;

    [Tooltip("Count and name of each Game Level (scene)")]
    public string[] gameLevels; //names of levels
    [HideInInspector]
    public int gameLevelsCount; //what level we are on
    private int loadLevel; //what level from the array to load

    public static string currentSceneName; //the current scene name;

    [Header("FOR TESTING")]
    public bool nextLevel = false; //test for next level

    //Game State Varaiables
    [HideInInspector] public enum gameStates { Idle, Playing, Death, GameOver, BeatLevel };//enum of game states
    [HideInInspector] public gameStates gameState = gameStates.Idle;//current game state

    //Timer Varaibles
    private float currentTime; //sets current time for timer
    private bool gameStarted = false; //test if games has started

    //Win/Loose conditon
    [SerializeField] //to test in inspector
    private bool playerWon = false;

    //reference to system time
    private static string thisDay = System.DateTime.Now.ToString("yyyy"); //today's date as string


    /*** METHODS ***/

    //Awake is called when the game loads (before Start).  Awake only once during the lifetime of the script instance.
    void Awake()
    {
        if (highScore <= 0)
        {
            highScore = 0;
        }

        //PlayerPrefs.SetInt("HighScore", testHS); TEST HS
        //runs the method to check for the GameManager
        CheckGameManagerIsInScene();

        //store the current scene
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log(gameCredit);

        moles = new List<GameObject>();
        playerHits = new List<GameObject>();

        GetHighScore();
        
    }//end Awake()

    // Update is called once per frame
    private void Update()
    {
        
        //if ESC is pressed , exit game. Since we're doing a webGL build, a browser game, this is unnecesary.
        //if (Input.GetKey("escape")) { ExitGame(); }

        //If Time is up. Just to check timer and update it. SHOULD ONLY COUNT DOWN ONCE GAME STARTED/IS IN LEVEl
        if (gameStarted && sequencing == false) { TimeCheck(); }
        
        //Check if player's hit moles match. Alternatively, do this when the player hits the moles and not per frame
        //checkMoles();

        //Check for next level
        if (nextLevel) { NextLevel(); }

        //if we are playing the game
        if (gameState == gameStates.Playing)
        {
            //if we have died and have no more lives, go to game over
            if (levelLost && (lives == 0)) { GameOver(); }

        }//end if (gameState == gameStates.Playing)



    }//end Update

    //Load the Help Screen
    public void GoHelp()
    {
        SceneManager.LoadScene(helpScene);
    } //end GoHelp()

    //Go back to menu
    public void GoBack()
    {
        SceneManager.LoadScene(startScene);
    }

    //LOAD THE GAME FOR THE FIRST TIME OR RESTART
    public void StartGame()
    {
        CheckScore();
        //SET ALL GAME LEVEL VARIABLES FOR START OF GAME. Considering this is the first level, let's have the "difficulty" be not so high.
        score = 0; //Set starting score

        MoleCrafter.maxMoles = 4; //4 is not too hard. Second level onwards will have eight.
        time = 35.0f;

        gameLevelsCount = 1; //set the count for the game levels
        loadLevel = gameLevelsCount - 1; //the level from the array
        SceneManager.LoadScene(gameLevels[loadLevel]); //load first game level

        gameState = gameStates.Playing; //set the game state to playing

        lives = numberOfLives; //set the number of lives

        endMsg = defaultEndMessage; //set the end message default

        playerWon = false; //set player winning condition to false
        Debug.Log("Game started");
        moles.Clear();
        playerHits.Clear();
        InitializeMoles();
        gameStarted = true; //The timer can start. This also will freeze and reset when the player wins or loses.
        
    }//end StartGame()

    //If player loses a life but has not lost the game. Reload the level after playing the lose animation for the PC
    public void LoseALife()
    {
        if (lives > 0)
        {
            lives--;
            levelLost = true;
            SceneManager.LoadScene(gameLevels[loadLevel]);
            levelLost = false;
            time = 30.0f; //Reset the timer

            //Reset lists
            moles.Clear();
            playerHits.Clear();
            InitializeMoles();
            //MoleCrafter.moleSeq = 0; If it doesn't reset after restart
        } //end if (lives > 0)
        else
        {
            GameOver();
        } //end else
    } //end LoseALife()


    //Moles are for moles themselves and MoleHoles for the gameobjects holding them. MoleCrafter has the bulk of the script.
    public void InitializeMoles()
    {
        sequencing = true;
        Debug.Log("Now sequencing");
       
    } //end initializeMoles()

    //Check if the contents of playerHits matches that of the first moles
    public void CheckMoles()
    {
        for (int i = 0; i < playerHits.Count; i++)
        {
            //Assume the player correctly hit
            playerHits[i].GetComponent<Mole>().hitCorrect = true;

            if (playerHits[i] != moles[i]) //Will only go as far as the player has made hits
            {
                PlayerCharacter.LoseLevel();
                playerHits[i].GetComponent<Mole>().hitCorrect = false;
                playerHits[i].GetComponent<Mole>().hitIncorrect = true; //Correct them if it isn't
            } //end if (playerHits[i] != moles[i])

            if (playerHits.Count == moles.Count && playerHits[i] == moles[i])
            {
                PlayerCharacter.WinLevel();
            } //end if (playerHits.Count == moles.Count && playerHits[i] == moles[i])
        } //end for loop


    } //end CheckMoles()

    /*
    //EXIT THE GAME
    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Exited Game");
    }//end ExitGame() 
    */


    //GO TO THE GAME OVER SCENE
    public void GameOver()
    {
        gameStarted = false;
        time = 30.0f;
        gameState = gameStates.GameOver; //set the game state to gameOver

       if(playerWon) { endMsg = winMessage; } else { endMsg = looseMessage; } //set the end message

        SceneManager.LoadScene(gameOverScene); //load the game over scene
        Debug.Log("Gameover");
    } //end GameOver()
    
    
    //GO TO THE NEXT LEVEL
    public void NextLevel()
    {
        nextLevel = false; //reset the next level
        score = score + (10*(Mathf.FloorToInt(time % 60)) + (100*MoleCrafter.maxMoles) + gameLevelsCount); //Add score ONLY if player has won. It also goes up with number of moles

        CheckScore();
        Debug.Log("High Score: " + highScore);
        Debug.Log("High Score: " + PlayerPrefs.GetInt("HighScore"));

        //as long as our level count is not more than the amount of levels
        if (gameLevelsCount < gameLevels.Length)
        {
            playerWon = false;
            gameLevelsCount++; //add to level count for next level
            loadLevel = gameLevelsCount - 1; //find the next level in the array
            SceneManager.LoadScene(gameLevels[loadLevel]); //load next level

            if (MoleCrafter.maxMoles < 8)
            {
                MoleCrafter.maxMoles = MoleCrafter.maxMoles + 2;
            } //end (if MoleCrafter.maxMoles < 8)
            else
            {
                MoleCrafter.maxMoles = 8;
            } //end else
            time = 35.0f; //Reset the timer
            //Reset lists
            moles.Clear();
            playerHits.Clear();
            InitializeMoles();

        } //end (if gameLevelsCount < gameLevels.Length)

        else{ //if we have run out of levels go to game over. Clearly in this case, the player has won the game.
            playerWon = true;
            GameOver();
        } //end if (gameLevelsCount <=  gameLevels.Length)

    }//end NextLevel()

    public void TimeCheck()
    {
        if (time <= 0) {
            playerWon = false;
            PlayerCharacter.LoseLevel();
        } //end if
        else
        {
            time -= Time.deltaTime;
        } //end else
    } //end TimeCheck();

    void CheckScore()
    { //Checks score on update and compares it to the high score
        if (score > highScore)
        {
            highScore = score;

            PlayerPrefs.SetInt("HighScore", highScore); //Set playerPrefs
        } //end if

        PlayerPrefs.Save();
    } //end CheckScore()

    void GetHighScore() 
    {
        //If PlayerPrefs already has a HighScore get that
        if (PlayerPrefs.HasKey("HighScore"))
        {
            Debug.Log("We already have a high score: " + PlayerPrefs.GetInt("HighScore"));
            if (PlayerPrefs.GetInt("High Score") > highScore)
            {
                highScore = PlayerPrefs.GetInt("High Score"); //Set high score to that
            }
            highScore = PlayerPrefs.GetInt("HighScore");
        } //end if

        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    } //end GetHighScore()


}
