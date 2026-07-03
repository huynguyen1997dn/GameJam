using SRDebugger;
using UnityEngine;
using System.ComponentModel; // Bắt buộc phải có để dùng [Category]

public class SRDebugManager : Singleton<SRDebugManager>
{
    private MiniGameType _miniGameType;

    // 1. Chuyển thành Property để SRDebugger tự động tạo Dropdown chọn Enum
    [Category("Mini Game Debugger")]
    public MiniGameType MiniGame
    {
        get => _miniGameType;
        set => _miniGameType = value;
    }

    private void Start()
    {
        // 2. Đăng ký class này vào hệ thống của SRDebugger khi game chạy
        SRDebug.Instance.AddOptionContainer(this);
    }

    private void OnDestroy()
    {
        // 3. Hủy đăng ký khi object bị xóa để tránh rò rỉ bộ nhớ (Memory Leak)
        if (SRDebug.Instance != null)
        {
            SRDebug.Instance.RemoveOptionContainer(this);
        }
    }

    // 4. Hàm Play sẽ tự động biến thành một nút bấm (Button) trong bảng SRDebugger
    [Category("Mini Game Debugger")]
    public void Play()
    {
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.StartGame(_miniGameType, null);
            Debug.Log($"[SRDebug] Đang chạy game: {_miniGameType}");
        }
        else
        {
            Debug.LogError("[SRDebug] MiniGameManager.Instance đang bị NULL!");
        }
    }
}