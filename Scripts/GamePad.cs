/*!
 *  @file GamePad.cs
 *  @brief ゲームパッド入力管理クラス
 *  @date 2017/04/11
 *  @author 仁科香苗
 */
using System.Collections;
using UnityEngine;

namespace InputGamePad
{
    public static class GamePad
    {
        public enum Button { A, B, Start, Dash, Jump, Decide, Cancel }                          //ボタン
        public enum Trigger { LeftTrigger, RightTrigger, L_Scissors, R_Scissors }     //トリガー
        public enum Stick { AxisX, AxisY }    //スティック
        private static Vector2 preLeftStick = Vector2.zero;
        private const float stickMiddle = 0.5f;

        //ボタンを押した瞬間
        public static bool GetButtonDown(Button button)
        {
            KeyCode code = GetKeyCode(button);
            return Input.GetKeyDown(code);
        }

        //ボタンを離した瞬間
        public static bool GetButtonUp(Button button)
        {
            KeyCode code = GetKeyCode(button);
            return Input.GetKeyUp(code);
        }

        //ボタンを押している間
        public static bool GetButton(Button button)
        {
            KeyCode code = GetKeyCode(button);
            return Input.GetKey(code);
        }

        //左スティックの入力状態
        public static Vector2 GetLeftStickAxis(bool raw)
        {
            Vector2 axis = Vector3.zero;

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)
                || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
            {
                if (Input.GetKey(KeyCode.LeftArrow)) axis.x = -1f;
                else if (Input.GetKey(KeyCode.RightArrow)) axis.x = 1f;
                if (Input.GetKey(KeyCode.UpArrow)) axis.y = 1f;
                else if (Input.GetKey(KeyCode.DownArrow)) axis.y = -1f;
                return axis;
            }
            try
            {
                if (!raw)
                {
                    axis.x = Input.GetAxis("L_XAxis");
                    axis.y = Input.GetAxis("L_YAxis") * -1;
                }
                else
                {
                    axis.x = Input.GetAxisRaw("L_XAxis");
                    axis.y = Input.GetAxisRaw("L_YAxis") * -1;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                Debug.LogWarning("InputError:LeftStick");
            }
            return axis;
        }

        //左スティックを入力した瞬間
        public static float GetLeftStickAxis(bool raw, Stick axis)
        {
            Vector2 now = GetLeftStickAxis(raw);
            float diff = 0f;
            if (axis == Stick.AxisX)
            {
                diff = Mathf.Abs(preLeftStick.x - now.x);
            }
            else if (axis == Stick.AxisY)
            {
                diff = Mathf.Abs(preLeftStick.x - now.x);
            }

            if (diff < stickMiddle)
                return 0f;
            else
                preLeftStick = now;

            if (axis == Stick.AxisX)
                return (now.x >= 0) ? 1f : -1f;
            else if (axis == Stick.AxisY)
                return (now.y >= 0) ? 1f : -1f;

            return 0f;
        }

        //左スティックのY軸を入力した瞬間

        //トリガー入力状態
        public static float GetTrigger(Trigger trigger, bool raw)
        {
            string name = "";
            if (trigger == Trigger.LeftTrigger || trigger == Trigger.L_Scissors)
            {
                name = "TriggersL";
            }
            else if (trigger == Trigger.RightTrigger || trigger == Trigger.R_Scissors)
            {
                name = "TriggersR";
            }

            float axis = 0f;
            try
            {
                if (raw)
                {
                    axis = Input.GetAxis(name);
                }
                else
                {
                    axis = Input.GetAxisRaw(name);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                Debug.LogWarning("InputError:" + name);
            }
            return axis;
        }

        //ゲームパッドの押下状態
        public static GamePadState GetState(bool raw)
        {
            GamePadState state = new GamePadState();

            state.A = GetButton(Button.A);
            state.B = GetButton(Button.B);
            state.Start = GetButton(Button.Start);
            state.Jump = GetButton(Button.Jump);
            state.Decide = GetButton(Button.Decide);
            state.Cancel = GetButton(Button.Cancel);
            state.LeftTrigger = GetTrigger(Trigger.LeftTrigger, raw);
            state.RightTrigger = GetTrigger(Trigger.RightTrigger, raw);
            state.L_Scissors = GetTrigger(Trigger.L_Scissors, raw);
            state.R_Scissors = GetTrigger(Trigger.R_Scissors, raw);
            state.LeftStick = GetLeftStickAxis(raw);

            return state;
        }

        //入力したキーを返す
        static KeyCode GetKeyCode(Button button)
        {
            switch (button)
            {
                case Button.A: return KeyCode.Joystick1Button0;
                case Button.B: return KeyCode.Joystick1Button1;
                case Button.Start: return KeyCode.Joystick1Button7;

                case Button.Dash: return KeyCode.Joystick1Button1;
                case Button.Jump:
                    if (Input.GetKeyDown(KeyCode.Space))
                        return KeyCode.Space; return KeyCode.Joystick1Button0;
                case Button.Decide: return KeyCode.Joystick1Button0;
                case Button.Cancel: return KeyCode.Joystick1Button1;
            }
            return KeyCode.None;
        }
    }

    //ゲームパッドの状態
    public class GamePadState
    {
        public bool A = false;
        public bool B = false;
        public bool Start = false;
        public bool Dash = false;
        public bool Jump = false;
        public bool Decide = false;
        public bool Cancel = false;
        public float LeftTrigger = 0f;
        public float RightTrigger = 0f;
        public float L_Scissors = 0f;
        public float R_Scissors = 0f;
        public Vector2 LeftStick = Vector2.zero;
    }
}