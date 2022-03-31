using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class managerScript : MonoBehaviour
{
    public GameObject TilemapObj;
    public GameObject activeTilemapObj;
    public GameObject scoreBoard;

    public AudioClip se_tileset;
    public AudioClip se_tileeval;
    public AudioClip se_snake_chdir;

    private int score = 0;

    public Text scoreText;

    public TileBase[] tileLight;
    public TileBase[] tileDark;
    public TileBase tileDisabled;

    private int tileTypeN;

    private AudioSource sound;
    private AudioSource sound_small;

    private Tilemap mapTile;
    private Tilemap mapTileActive;

    // Start is called before the first frame update
    void Start()
    {
        sound = GetComponent<AudioSource>();
        sound_small = GetComponent<AudioSource>();

        mapTile = TilemapObj.GetComponent<Tilemap>();
        mapTileActive = activeTilemapObj.GetComponent<Tilemap>();

        scoreText = scoreBoard.GetComponent<Text>();

        tileTypeN = tileLight.Length;
        if(tileDark.Length != tileTypeN){
            throw new UnityException("not same tile type number");
        }
        GameInit();
        ToGameTimeLoop();
    }

    IEnumerator LoopCoroutine;

    IEnumerator GameTimeLoop()
    {
        while(true){
            GameUpdate();
            yield return new WaitForSeconds(0.15f);
        }
    }

    private const int mapWidth = 8;
    private const int mapHeight = 14;

    private const int chBorder = 5;
    private int[,] map = new int[mapWidth, mapHeight];

    private Snake snake;

    void GameInit()
    {
        for(int x = 0; x < mapWidth; x++){
            for(int y = 0; y < mapHeight; y++){
                map[x, y] = -1;
            }
        }
    }

    void SyncTileDat(){
        for(int x = 0; x < mapWidth; x++){
            for(int y = 0; y < mapHeight; y++){
                var pos = new Vector3Int(x, -y, 0);

                mapTileActive.SetTile(pos, null);
                if(map[x, y] == -1){
                    mapTile.SetTile(pos, null);
                }else{
                    mapTile.SetTile(pos, tileDark[map[x, y]]);
                }
            }
        }

        if(snake != null){
            snake.Draw(mapTileActive, tileLight);
        }
    }

    public bool TileCollision(int x, int y){
        if( x < 0 || x >= mapWidth ||
            y >= mapHeight){
            return true;
        }

        if(y >= 0 && map[x, y] != -1){
            return true;
        }

        return false;
    }

    private void ToGameTimeLoop(){
        if(LoopCoroutine != null)
            StopCoroutine(LoopCoroutine);
        LoopCoroutine = GameTimeLoop();
        StartCoroutine(LoopCoroutine);
    }

    private void ToEvalLoop(){
        StopCoroutine(LoopCoroutine);
        LoopCoroutine = EvalLoop();
        StartCoroutine(LoopCoroutine);
    }

    void GameUpdate()
    {
        if(snake == null){
            snake = Snake.nextSnake(tileTypeN);
        } else {
            if(!snake.Move(TileCollision)){

                int maxHeight = mapHeight;

                int fallY = 0;
                while(true){
                    int nextFallY = fallY + 1;

                    bool ok = true;
                    foreach (var part in snake.parts)
                    {
                        if(TileCollision(part.x, part.y + nextFallY)){
                            ok = false;
                            break;
                        }
                    }
                    if(ok){
                        fallY = nextFallY;
                    } else {
                        break;
                    }
                }


                foreach (var part in snake.parts)
                {
                    maxHeight = Mathf.Min(maxHeight, part.y + fallY);
                    map[part.x, part.y + fallY] = part.type;
                }
                snake = null;                
                sound.Stop();
                sound.PlayOneShot(se_tileset);

                if(maxHeight < chBorder){
                    ToEvalLoop();
                }
            }
        }
        
        SyncTileDat();
    }

    IEnumerator EvalLoop(){

        var doneMap = new bool[mapWidth, mapHeight];

        int gainSum = 0;

        for (int y = mapHeight - 1; y >= 0 ; y--){
            for (int x = 0; x < mapWidth; x++){
                if(doneMap[x, y]){
                    continue;
                }

                var nowType = map[x, y];
                if(nowType == -1){
                    continue;
                }

                var detectedTiles = new bool[mapWidth, mapHeight];
                var detectedTilesList = new List<Vector2Int>();

                var que = new Queue<Vector2Int>();
                que.Enqueue(new Vector2Int(x, y));

                var typeSet = new HashSet<int>();

                while(que.Count != 0){
                    var next = que.Dequeue();
                    if(!(0 <= next.x && next.x < mapWidth &&
                    0 <= next.y && next.y < mapHeight)){
                        continue;
                    }

                    if(doneMap[next.x, next.y]){
                        continue;
                    }

                    if(map[next.x, next.y] == nowType){
                        detectedTiles[next.x, next.y] = true;
                        detectedTilesList.Add(new Vector2Int(next.x, next.y));
                        doneMap[next.x, next.y] = true;
                        
                        que.Enqueue(new Vector2Int(next.x + 1, next.y));
                        que.Enqueue(new Vector2Int(next.x - 1, next.y));
                        que.Enqueue(new Vector2Int(next.x, next.y + 1));
                        que.Enqueue(new Vector2Int(next.x, next.y - 1));
                    }else{
                        typeSet.Add(map[next.x, next.y]);
                    }
                }

                foreach (var detectedTile in detectedTilesList)
                {
                    Debug.Log(detectedTile);
                    mapTile.SetTile(
                        new Vector3Int(detectedTile.x, -detectedTile.y, 0),
                        tileLight[nowType]);
                }

                sound.PlayOneShot(se_tileeval);

                int gain = detectedTilesList.Count * 10 + 50 + typeSet.Count * 30;
                gainSum += gain;

                scoreText.text += "+";
                scoreText.text += gain.ToString();
                yield return new WaitForSeconds(1.0f);

                foreach (var detectedTile in detectedTilesList)
                {
                    mapTile.SetTile(
                        new Vector3Int(detectedTile.x, -detectedTile.y, 0),
                        tileDark[nowType]);
                }
            }
        }

        score += gainSum;
        scoreText.text = score.ToString();

        int emptyC = 0;
        for (int y = chBorder; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if(map[x,y] == -1){
                    emptyC++;
                }
                map[x, y] = -1;
            }
        }

        for (int y = mapHeight - 1; y >= 1; y--)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                map[x, y] = -1;
            }
        }

        bool gameover = (float)emptyC / ((mapHeight - chBorder) * mapWidth) > 0.4f;

        if(gameover){
            scoreText.text = "density < 60%";
            yield return new WaitForSeconds(3.0f);
        }

        for (int i = 0; i < mapHeight; i++)
        {
            for (int y = mapHeight - 1; y >= 1; y--)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    mapTile.SetTile(
                        new Vector3Int(x, -y, 0),
                        mapTile.GetTile(
                            new Vector3Int(x, -(y - 1), 0)
                        )
                    );
                }
            }

            yield return new WaitForSeconds(0.05f);
        }

        if(gameover){
            scoreText.text = "GAME OVER";
            yield return new WaitForSeconds(30.0f);
        }

        ToGameTimeLoop();
    }

    // Update is called once per frame
    void Update()
    {
        float ndx = Input.GetAxisRaw("Horizontal");
        float ndy = Input.GetAxisRaw("Vertical");  // unity座標系(y軸正=上)

        int dir = -1;

        if(ndx != 0 || ndy != 0){
            if(ndy == -1){
                dir = 2;
            }
            else if(ndx == -1){
                dir = 3;
            }
            else if(ndx == 1){
                dir = 1;
            }
            else if(ndy == +1){
                dir = 0;
            }
            if(snake != null){
                if(snake.setDirection(dir)){
                    // sound.Stop();
                    // sound.PlayOneShot(se_snake_chdir, 0.2f);
                }
            }
        }
    }
}
