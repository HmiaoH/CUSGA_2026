using Frameworks;
using UnityEngine;

namespace Managers
{
    public class GameFlowManager : ManagerBase<GameFlowManager>
    {
        [Header("剧情对话")]
        [SerializeField] private DialogueSequenceSO openingDialogue;

        public GameState CurrentState { get; private set; } = GameState.None;

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
            //debug
            Debug.Log("Current State: " + CurrentState);
            Debug.Log("Enter State: " + newState);

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
        /// 进入某个状态
        /// </summary>
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
                    EnterDialogue();
                    break;
            }
        }

        /// <summary>
        /// 进入开场动画状态，播放开场动画。
        /// 动画播放完毕后进入对话状态。
        /// </summary>
        private void EnterOpeningCutscene()
        {
            // TODO: 开场动画恢复后，改为：
            // CutsceneManager.Instance.PlayCutscene(
            //     Utils.CutsceneName.OPENING,
            //     () => ChangeState(GameState.Dialogue)
            // );

            // 当前没有开场动画，直接进入对话
            ChangeState(GameState.Dialogue);
        }

        /// <summary>
        /// 进入对话状态，播放开场剧情对话。
        /// 对话播放完毕后进入 PlayerTurnStart。
        /// </summary>
        private void EnterDialogue()
        {
            DialogueManager.Instance.StartDialogue(
                openingDialogue,
                () => ChangeState(GameState.PlayerTurnStart)
            );
        }
    }
}