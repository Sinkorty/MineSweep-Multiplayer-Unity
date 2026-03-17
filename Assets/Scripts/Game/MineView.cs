using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MineView : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;

    [SerializeField] private List<TileBase> cellList;
    public MineModule model;

    public void SetCell(int x, int y, int index)
    {
        if (model == null) // 违反单一职责原则的后果 
            model = GetComponent<MineController>().GetModel();

        // [1, maxLength]
        if (x < 1 || y < 1 || x > model.MaxWidth || y > model.MaxHeight) return;
        if (index >= 0 && index < cellList.Count)
        {
            tilemap.SetTile(new Vector3Int(x, y, 0), cellList[index]);
        }
    }
    public int GetCell(int x, int y)
    {
        if (model == null) // 违反单一职责原则的后果 
            model = GetComponent<MineController>().GetModel();


        TileBase cell = tilemap.GetTile(new Vector3Int(x, y, 0));
        if (cell == null)
        {
            throw new System.Exception("Tile is empty");
        }
        return cellList.IndexOf(cell);
    }
    // 根据 Module 的棋盘数据刷新棋盘
    public void RefreshView()
    {
        if (model == null) // 违反单一职责原则的后果 
            model = GetComponent<MineController>().GetModel();


        for (int x = 1; x <= model.MaxWidth; x++)
        {
            for (int y = 1; y <= model.MaxHeight; y++)
            {
                //if (!model.IsVeiled[x, y])
                //{
                //    int index = model.ToIndex(0, true);
                //    if (model.IsFlaged[x, y])
                //    {

                //    }
                //    else
                //    {
                //        SetCell(x, y, index);
                //    }
                //}
                //else
                //{
                //    SetCell(x, y, model.ToIndex(model.board[x, y], false));
                //}
                if (model.IsVeiled[x, y]) // 揭开了，要么显示空或数字
                {
                    SetCell(x, y, model.ToIndex(model.board[x, y], false));
                }
                else if (model.IsFlaged[x, y])
                {
                    SetCell(x, y, model.ToIndex(-2, true));
                }
                else
                {
                    SetCell(x, y, model.ToIndex(0, true));
                }
            }
        }
    }
}
