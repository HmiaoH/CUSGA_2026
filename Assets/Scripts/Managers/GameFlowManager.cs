using Frameworks;
namespace Managers
{
    public class GameFlowManager : ManagerBase<GameFlowManager>
    {
        public GameState CurrentState { get; private set; } =  GameState.None;

        // 初始时进入开场动画状态
        protected override void OnInit()
        {
            ChangeState(GameState.OpeningCutscene);
        }

        /// <summary>
        /// 切换某个状态，在Managers中调用这个函数
        /// </summary>
        /// <param name="newState">新的状态</param>
        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            ExitState(CurrentState);
            CurrentState = newState;
            EnterState(CurrentState);
        }

        /// <summary>
        /// 退出某个状态，每一种情况如果有操作，就在此脚本中新建一个Exitxxx()的函数，下面调用这个函数。
        /// </summary>
        /// <param name="state">退出的状态</param>
        private void ExitState(GameState state)
        {
            switch (state)
            {
                case GameState.None:
                    break;
                case GameState.OpeningCutscene:
                    break;
                case GameState.PlayerTurnStart:
                    break;
                case GameState.WaitingForCard:
                    break;
                case GameState.ResolvingCard:
                    break;
                case GameState.EnemyTurn:
                    break;
                case GameState.TableInteraction:
                    break;
                case GameState.LevelComplete:
                    break;
                case GameState.Dialogue:
                    break;
            }
        }

        /// <summary>
        /// 进入某个状态，每一种情况如果有操作，就在此脚本中新建一个Enterxxx()的函数，下面调用这个函数。
        /// </summary>
        /// <param name="state">进入的状态</param>
        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.None:
                    break;
                case GameState.OpeningCutscene:
                    EnterOpeningCutscene();
                    break;
                case GameState.PlayerTurnStart:
                    break;
                case GameState.WaitingForCard:
                    break;
                case GameState.ResolvingCard:
                    break;
                case GameState.EnemyTurn:
                    break;
                case GameState.TableInteraction:
                    break;
                case GameState.LevelComplete:
                    break;
                case GameState.Dialogue:
                    break;
            }
        }

        /// <summary>
        /// 进入开场动画状态，播放开场动画。
        /// </summary>
        private void EnterOpeningCutscene()
        {
            return;
            // CutsceneManager.Instance.PlayCutscene(Utils.CutsceneName.OPENING, null);
        }

    }
}
