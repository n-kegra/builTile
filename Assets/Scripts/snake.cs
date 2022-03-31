using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

class Snake{
    public struct SnakeBody{
        public int x, y;
        public int type;
    }

    public SnakeBody[] parts { get; }
    int dx, dy;// ゲーム座標系(y軸正＝下)

    private Snake(SnakeBody[] _parts){
        parts = _parts;
        dx = 0;
        dy = 1;
    }

    public void Draw(Tilemap tilemap, TileBase[] tiles){
        foreach (var snakePart in parts){
            tilemap.SetTile(new Vector3Int(snakePart.x, -snakePart.y, 0), tiles[snakePart.type]);
        }
    }


    // if ok returns true
    public bool setDirection(int dir){
        int oldDx = dx, oldDy = dy;

        switch(dir){
            case 0:
                dx = 0; dy = -1;
                break;
            case 1:
                dx = 1; dy = 0;
                break;
            case 2:
                dx = 0; dy = +1;
                break;
            case 3:
                dx = -1; dy = 0;
                break;
        }

        // if(!CanMove()){
        //     dx = oldDx;
        //     dy = oldDy;
        //     return false;
        // }

        if(dx == -oldDx && dy == -oldDy){
            dx = oldDx;
            dy = oldDy;
            return false;
        }

        return true;
    }

    // if movable returns true
    private bool CanMove(System.Func<int, int, bool> func = null){
        var to = new Vector2Int(parts[0].x + dx, parts[0].y + dy);

        if(func(to.x, to.y)){
            return false;
        }

        foreach (var part in parts)
        {
            if(part.x == to.x && part.y == to.y){
                return false;
            }
        }

        return true;
    }

    // if ok returns true;
    public bool Move(System.Func<int, int, bool> func = null){
        if(!CanMove(func)){
            return false;
        }

        for (int i = parts.Length - 2; i >= 0; i--)
        {
            parts[i+1].x = parts[i].x;
            parts[i+1].y = parts[i].y;
        }
        parts[0].x += dx;
        parts[0].y += dy;

        return true;
    }

    public static Snake nextSnake(int tileTypeN){
        SnakeBody[] nextSnakeBody = new SnakeBody[4];

        int type = Random.Range(0, tileTypeN);
        for (int i = 0; i < nextSnakeBody.Length; i++)
        {
            nextSnakeBody[i].x = 0;
            nextSnakeBody[i].y = -i;
            nextSnakeBody[i].type = type;
        }

        return new Snake(nextSnakeBody);
    }
}