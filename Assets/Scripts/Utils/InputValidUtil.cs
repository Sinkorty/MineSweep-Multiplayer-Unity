using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 关于输入的有效性验证工具类
/// </summary>
public static class InputValidUtil
{
    public static bool CheckPlayerName(string content, out string err)
    {
        if (ContainsChinese(content))
        {
            err = "Contains Chinese character, please remove it due to limited size of font material.";
            return false;
        }
        if (content.Length <= 3)
        {
            err = "Your name is too short";
            return false;
        }
        if (content.Length > 16)
        {
            err = "Your name is too long";
            return false;
        }
        foreach (char c in "!@#$%^&*()~`-=+[]{};\':\"\\|,./<>?")
        {
            if (content.Contains(c))
            {
                err = "Invaild character in your name: " + c;
                return false;
            }
        }
        err = string.Empty;
        return true;
    }
    public static bool CheckSizeAndMineCount(string widthText, string heightText, string mineCountText, out string err, out int w, out int h, out int c)
    {
        w = h = c = 0;
        if (!int.TryParse(widthText, out int width))
        {
            err = "width input invalid.";
            return false;
        }
        if (!int.TryParse(heightText, out int height))
        {
            err = "height input invalid.";
            return false;
        }
        if (!int.TryParse(mineCountText, out int mineCount))
        {
            err = "mine num input invalid.";
            return false;
        }
        if (width < 0 || width > 1024)
        {
            err = "width is too long or negative";
            return false;
        }
        if (height < 0 || height > 1024)
        {
            err = "height is too long or negative";
            return false;
        }
        if (mineCount >= width * height - 5)
        {
            err = "too many mines";
            return false;
        }
        w = width; h = height; c = mineCount;
        if (c > w * h - 9)
        {
            err = "too many mines";
            return false;
        }


        err = string.Empty;
        return true;
    }
    public static bool CheckHostAndPort(string hostStr, string portStr, out string err)
    {
        //// 不检查 host了
        //if (!IPAddress.TryParse(hostStr, out IPAddress address) || (address.AddressFamily != AddressFamily.InterNetwork &&
        //    address.AddressFamily != AddressFamily.InterNetworkV6))
        //{
        //    err = "host input invalid.";
        //    return false;
        //}
        if (!int.TryParse(portStr, out int port))
        {
            err = "port should be a number";
            return false;
        }
        if (port < 1 || port > 65535)
        {
            err = "port should between 1 and 65535";
            return false;
        }
        err = string.Empty;
        return true;
    }
    // 不要说中文（TMPRO会爆字符集的）
    public static bool ContainsChinese(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // 匹配中文字符的Unicode范围
        Regex regex = new Regex(@"[\u4e00-\u9fff]");
        return regex.IsMatch(text);
    }
}
