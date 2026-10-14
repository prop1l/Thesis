namespace Thesis.Models;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

/// <summary>Настройки приложения, которые сохраняются между запусками.</summary>
public class AppSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public string AccentColor { get; set; } = "#4F46E5";
}
