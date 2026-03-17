using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MineModule
{
    public int[,] board;

    public bool[,] IsVeiled { get; private set; }
    public bool[,] IsFlaged { get; private set; }

    public int MaxWidth { get; private set; }
    public int MaxHeight { get; private set; }
    public int MineNum { get; private set; }
    public int FlagNum { get; private set; }
    public int VeilNum { get; private set; }


    public MineModule(int maxWidth, int maxHeight, int mineNum)
    {
        if (mineNum > maxWidth * maxHeight)
        {
            mineNum = maxWidth * maxHeight - 1;
        }
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        MineNum = Mathf.Min(maxHeight * maxHeight - 9, mineNum);

        board = new int[maxWidth + 2, maxHeight + 2];
        IsVeiled = new bool[maxWidth + 2, maxHeight + 2];
        IsFlaged = new bool[maxWidth + 2, maxHeight + 2];

        // 初始化board
        for (int x = 0; x < board.GetLength(0); x++)
        {
            for (int y = 0; y < board.GetLength(1); y++)
            {
                board[x, y] = 0;
                IsVeiled[x, y] = false;
                IsFlaged[x, y] = false;
            }
        }
    }
    // 为创建者提供
    public void SummonMines(int firstX, int firstY)
    {
        // 初始化雷
        int total = 0;
        while (true)
        {
            int x = Random.Range(1, MaxWidth + 1);
            int y = Random.Range(1, MaxHeight + 1);

            // 判断是否在初次点击类的周围八个格子内，计算机对于Sqrt计算效率是很高的，无需担心
            bool nearFirst = Mathf.Sqrt((firstX - x) * (firstX - x) + (firstY - y) * (firstY - y)) < 1.5;

            if (board[x, y] != -1 && !(x == firstX && y == firstY) && !nearFirst) // 并未重复
            {
                // 生成雷，周围数字+1
                board[x, y] = -1;
                if (board[x - 1, y - 1] != -1) board[x - 1, y - 1] += 1;
                if (board[x, y - 1] != -1) board[x, y - 1] += 1;
                if (board[x + 1, y - 1] != -1) board[x + 1, y - 1] += 1;
                if (board[x + 1, y] != -1) board[x + 1, y] += 1;
                if (board[x + 1, y + 1] != -1) board[x + 1, y + 1] += 1;
                if (board[x, y + 1] != -1) board[x, y + 1] += 1;
                if (board[x - 1, y + 1] != -1) board[x - 1, y + 1] += 1;
                if (board[x - 1, y] != -1) board[x - 1, y] += 1;
                total++;
            }
            if (total == MineNum) break;
        }
    }
    // cell: 1-8代表数字，0代表空格，-1代表雷，-2代表标记，将其转化为视图中的索引
    public int ToIndex(int cell, bool isUnveiled = true)
    {
        int index = 0;
        if (cell >= 1 && cell <= 8)
        {
            index = cell - 1;
        }
        else if (cell == 0) index = 8;
        else if (cell == -1) index = 10;
        else if (cell == -2) index = 9;

        if (!isUnveiled) index += 11;
        return index;
    }
    public void SetFlag(int x, int y)
    {
        IsFlaged[x, y] = !IsFlaged[x, y];
        FlagNum += IsFlaged[x, y] ? 1 : -1;
    }
    public void SetVeil(int x, int y, bool b)
    {
        IsVeiled[x, y] = b;
        VeilNum += IsVeiled[x, y] ? 1 : -1;
    }
    public bool IsWin()
    {
        return FlagNum == MineNum || MaxWidth * MaxHeight - MineNum == VeilNum;
    }
    public List<int> EncodeProgress() // 0: Unveiled & Unflaged  1: veiled  2: flaged
    {
        List<int> result = new List<int>();
        for (int y = 1; y <= MaxHeight; y++)
        {
            for (int x = 1; x <= MaxWidth; x++)
            {
                int toFill = 0;
                if (IsVeiled[x, y]) toFill = 1;
                if (IsFlaged[x, y]) toFill = 2;
                result.Add(toFill);
            }
        }
        return result;
    }
    public List<int> EncodeState()
    {
        List<int> result = new List<int>();
        for (int y = 1; y <= MaxHeight; y++)
        {
            for (int x = 1; x <= MaxWidth; x++)
            {
                result.Add(board[x, y]);
            }
        }
        return result;
    }
    public void DecodeState(List<int> state)
    {
        int curX = 1; int curY = 1;
        foreach (var item in state)
        {
            board[curX, curY] = item;
            curX++;
            if (curX == MaxWidth + 1) // 换行
            {
                curX = 1;
                curY += 1;
            }
        }
    }
    public void DecodeProgress(List<int> progress)
    {
        int curX = 1; int curY = 1;
        foreach (var item in progress)
        {
            if (item == 0)
            {
                IsVeiled[curX, curY] = false;
                IsFlaged[curX, curY] = false;
            }
            else if (item == 1)
            {
                if (!IsVeiled[curX, curY]) // 重复加载，都这样解决说明已经石山了
                {
                    VeilNum++;
                }
                IsVeiled[curX, curY] = true;
                IsFlaged[curX, curY] = false;
                
            }
            else if (item == 2)
            {
                if (!IsFlaged[curX, curY])
                {
                    FlagNum++;
                }
                IsVeiled[curX, curY] = false;
                IsFlaged[curX, curY] = true;
                
            }

            curX++;
            if (curX == MaxWidth + 1) // 换行
            {
                curX = 1;
                curY += 1;
            }
        }
    }

    public string getMineCount()
    {
        return (MineNum - FlagNum).ToString();
    }

    public string GetVeilNum()
    {
        return VeilNum.ToString();
    }
}
