using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        int[,] matriz = new int[Constants.NumTiles, Constants.NumTiles];

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriz[i, j] = 0;
            }
        }

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            int fila = i / Constants.TilesPerRow;
            int columna = i % Constants.TilesPerRow;

            if (fila > 0)
                matriz[i, i - Constants.TilesPerRow] = 1;
            if (fila < Constants.TilesPerRow - 1)
                matriz[i, i + Constants.TilesPerRow] = 1;
            if (columna > 0)
                matriz[i, i - 1] = 1;
            if (columna < Constants.TilesPerRow - 1)
                matriz[i, i + 1] = 1;
        }

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].adjacency.Clear();
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                if (matriz[i, j] == 1)
                    tiles[i].adjacency.Add(j);
            }
        }
    }


    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }

                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }
    }
    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        List<int> selectableTiles = new List<int>();
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (tiles[i].selectable)
            {
                selectableTiles.Add(i);
            }
        }

        int randomIndex = Random.Range(0, selectableTiles.Count);
        int randomTileIndex = selectableTiles[randomIndex];
        robber.GetComponent<RobberMove>().MoveToTile(tiles[randomTileIndex]);
        robber.GetComponent<RobberMove>().currentTile = tiles[randomTileIndex].numTile;
        
    }


    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexCurrentTile;

        if (cop)
            indexCurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexCurrentTile = robber.GetComponent<RobberMove>().currentTile;

        // La ponemos rosa porque acabamos de hacer un reset
        tiles[indexCurrentTile].current = true;

        // Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();
        nodes.Enqueue(tiles[indexCurrentTile]);

        while (nodes.Count > 0)
        {
            Tile currentNode = nodes.Dequeue();

            foreach (int adjacentIndex in currentNode.adjacency)
            {
                Tile adjacentTile = tiles[adjacentIndex];

                if (adjacentTile.numTile != indexCurrentTile)
                {
                    if (cop && adjacentTile.numTile == cops[1 - clickedCop].GetComponent<CopMove>().currentTile)
                    {
                        continue;
                    }

                    if (!cop && adjacentTile.numTile == cops[0].GetComponent<CopMove>().currentTile ||
                        adjacentTile.numTile == cops[1].GetComponent<CopMove>().currentTile)
                    {
                        continue;
                    }

                    if (!adjacentTile.visited)
                    {
                        adjacentTile.visited = true;
                        adjacentTile.parent = currentNode;
                        adjacentTile.distance = currentNode.distance + 1;

                        if (adjacentTile.distance <= Constants.Distance)
                        {
                            adjacentTile.selectable = true;
                            nodes.Enqueue(adjacentTile);
                        }
                    }
                }
            }
        }
    }
}