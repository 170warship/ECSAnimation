using Native.FSM;
using Native.Component;
using Native.Event;
using UnityEngine;
using System;

namespace Native.Procedure
{
	public class ProcedureMain : ProcedureBase
	{
		private int _mainUI;
		private Action _gotoGame;

		public override void OnInit(IFSM fsm)
		{
			base.OnInit(fsm);
			_mainUI = -1;
			_gotoGame = () =>
			{
				fsm.ChangeState<ProcedureGame>();
			};
        }

		public override void OnEnter(IFSM fsm)
		{
			base.OnEnter(fsm);
            _mainUI = LaunchComponent.UI.OpenUIForm(UIFormId.MainMenuUIForm);
            ECSBridgeManager.Instance.ClearAllEntity();

			//�ӳ�һ֡����ִ�������� ��ΪEcs��Update��Mono��Update˳���޷�����
			LaunchComponent.Scheduler.Scheduler_Frame(() =>
			{
				ECSBridgeManager.Instance.OnExitGame();
			}, 1);
        }

		public override void OnUpdate(IFSM fsm, float elpaseSecond)
		{
			base.OnUpdate(fsm, elpaseSecond);
		}

		public override void OnExit(IFSM fsm)
		{
			base.OnExit(fsm);
			LaunchComponent.UI.CloseUIForm(_mainUI);
			_mainUI = -1;
            ECSBridgeManager.Instance.OnEnterGame();
        }

		public void GoToGame()
		{
			_gotoGame?.Invoke();
        }
	}
}
