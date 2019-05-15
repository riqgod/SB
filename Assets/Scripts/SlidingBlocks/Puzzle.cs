using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Puzzle : MonoBehaviour
{
    public Texture2D image;
    public Texture2D fullProgressBar;
    public Texture2D emptyProgressBar;
    public int blocksPerLine = 4;
    public int shuffleLenght = 20;
    public float defaultMoveDuration = .125f;
    public float shuffleMoveDuration = .075f;

    Block[,] blocks;
    Block emptyBlock;
    Queue<Block> inputs;
    bool blockIsMoving;
    int shuffleMovesRemaining;
    Vector2Int previousShuffleOffset;
    public GameObject loadingScreen;
    public GameObject helpScreen;
    public GameObject puzzle;
    public GameObject levelCompleted;

    private float progressbar=0;
    private float actualProgress=0;

    enum PuzzleState { Solved, Shuffling, InPlay};
    PuzzleState state;


    private void Awake()
    {
        loadingScreen.SetActive(true);
        CreatePuzzle();
        StartShuffle();
        
    }

    void Update()
    {
        if (state == PuzzleState.Solved)
        {
            levelCompleted.SetActive(true);
        }
        if (state == PuzzleState.Solved && Input.GetKeyDown(KeyCode.Space))
        {
            StartShuffle();
        }
       
    }

    void OnGUI() {
        if (shuffleMovesRemaining > 1)
        {
            
            loadingScreen.SetActive(true);
            actualProgress = progressbar / (float)shuffleLenght;
            GUI.DrawTexture(new Rect(0, Screen.height/2, Screen.width, 50), emptyProgressBar);
            GUI.DrawTexture(new Rect(0, Screen.height/2, actualProgress * Screen.width, 50), fullProgressBar);
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, 0, 100, 50), string.Format("{0:N0}%",actualProgress * 100f));

        }
        else
        {
            loadingScreen.SetActive(false);
        }
    }

    void CreatePuzzle()
    {
        blocks = new Block[blocksPerLine, blocksPerLine];
        Texture2D[,] imageSlices = ImageSlicer.GetSlices(image, blocksPerLine);
        for(int y = 0; y < blocksPerLine; y++)
        {
            for (int x = 0; x < blocksPerLine; x++)
            {
                GameObject blockObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                blockObject.transform.position = -Vector2.one * (blocksPerLine - 1) * .5f + new Vector2(x, y);
                blockObject.transform.parent = transform;

                Block block = blockObject.AddComponent<Block>();
                block.OnBlockPressed += PlayerMoveBlockInput;
                block.OnFinishedMoving += OnBlockFinishedMoving;
                block.coord = new Vector2Int(x, y);
                block.Init(new Vector2Int(x, y), imageSlices[x, y]);
                blocks[x, y] = block;
                if (y == 0 && x == blocksPerLine -1)
                {
                    emptyBlock = block;
                }
            }
        }

        Camera.main.orthographicSize = blocksPerLine * .55f;
        inputs = new Queue<Block>();
    }

    void PlayerMoveBlockInput(Block blockToMove)
    {
        if (state == PuzzleState.InPlay)
        {
            inputs.Enqueue(blockToMove);
            MakeNextPlayerMove();
        }
    }

    void MakeNextPlayerMove()
    {
        while (inputs.Count > 0 && !blockIsMoving)
        {
            MoveBlock(inputs.Dequeue(), defaultMoveDuration);
        }
    }

    void MoveBlock(Block blockToMove, float duration)
    {
        if ((blockToMove.coord - emptyBlock.coord).sqrMagnitude == 1)
        {
            blocks[blockToMove.coord.x, blockToMove.coord.y] = emptyBlock;
            blocks[emptyBlock.coord.x, emptyBlock.coord.y] = blockToMove;

            Vector2Int targetCoord = emptyBlock.coord;
            emptyBlock.coord = blockToMove.coord;
            blockToMove.coord = targetCoord;


            Vector2 targetposition = emptyBlock.transform.position;
            emptyBlock.transform.position = blockToMove.transform.position;
            blockToMove.MoveToPosition(targetposition, duration);
            blockIsMoving = true;
        }
    }

    void OnBlockFinishedMoving()
    {
        blockIsMoving = false;
        CheckIfSolved();

        if (state == PuzzleState.InPlay)
        {
            MakeNextPlayerMove();
        }
        else if (state == PuzzleState.Shuffling)
        {
            if (shuffleMovesRemaining > 0)
            {
                MakeNextShuffleMove();
            }
            else
            {
                state = PuzzleState.InPlay;
            }
        }
    }

    void StartShuffle()
    {
        state = PuzzleState.Shuffling;
        shuffleMovesRemaining = shuffleLenght;
        emptyBlock.gameObject.SetActive(false);
        MakeNextShuffleMove();
    }

   

    public void ResetLevel()
    {
        
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
        
    }

    public void HelpDisable()
    {
        helpScreen.SetActive(false);
        foreach (Block bloc in blocks)
        {
            bloc.gameObject.GetComponent<MeshCollider>().enabled = true;
        }
    }

    void MakeNextShuffleMove()
    {
        Vector2Int[] offsets = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        int randomIndex = Random.Range(0, offsets.Length);

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2Int offset = offsets[(randomIndex + i) % offsets.Length];
            if(offset != previousShuffleOffset * -1)
            {
                Vector2Int moveBlockCoord = emptyBlock.coord + offset;

                if (moveBlockCoord.x >= 0 && moveBlockCoord.x < blocksPerLine && moveBlockCoord.y >= 0 && moveBlockCoord.y < blocksPerLine)
                {
                    MoveBlock(blocks[moveBlockCoord.x, moveBlockCoord.y], shuffleMoveDuration);
                    shuffleMovesRemaining--;
                    progressbar++;
                    previousShuffleOffset = offset;
                    break;
                }
            }
        }

    }

    public void HelpEnable()
    {
        if (!(state == PuzzleState.Solved))
        {
            helpScreen.SetActive(true);
            foreach(Block bloc in blocks)
            {
                bloc.gameObject.GetComponent<MeshCollider>().enabled = false;
            }
        }
    }

    void CheckIfSolved()
    {
        foreach(Block block in blocks)
        {
            if (!block.IsAtStartingCoord())
            {
                return;
            }
        }

        state = PuzzleState.Solved;
        emptyBlock.gameObject.SetActive(true);
    }
}
