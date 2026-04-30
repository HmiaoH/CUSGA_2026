namespace Frameworks
{
    public enum GameState
    {
        None,

        OpeningCutscene,     // 开场背景动画
        PlayerTurnStart,     // 玩家回合开始
        WaitingForCard,      // 等玩家选择/打出卡牌
        ResolvingCard,       // 正在结算卡牌效果
        EnemyTurn,           // 敌方行动
        LevelComplete,       // 关卡完成
        TableInteraction,    // 与桌面物体互动
        Dialogue,            // 剧情对话
        GameOver
    }
}
