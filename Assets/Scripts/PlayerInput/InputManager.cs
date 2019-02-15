using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InputManager {

    //Interface to represent the three properties of a button
    public interface IButton
    {
        bool IsHeld { get; }
        bool WasPressed { get; } 
        bool WasReleased { get; }
    }

    //Class for a button with a Keycode
    public class Button : InputManager.IButton
    {
        private KeyCode button;

        public Button(KeyCode button)
        {
            this.button = button;
        }

        public Button(string button)
        {
            this.button = (KeyCode)System.Enum.Parse(typeof(KeyCode), button);
        }

        public bool IsHeld { get{ return Input.GetKey(button); } }
        public bool WasPressed { get { return Input.GetKeyDown(button); } }
        public bool WasReleased { get { return Input.GetKeyUp(button); } }

        //Class that contains helper functions for checking if any of multiple buttons is pressed/released
        public class Any
        {
            public static bool IsHeld(params IButton[] buttons) {
                foreach (var b in buttons)
                    if (b.IsHeld) return true;
                return false;
            }
            public static bool WasPressed(params IButton[] buttons)
            {
                foreach (var b in buttons)
                    if (b.WasPressed) return true;
                return false;
            }
            public static bool WasReleased(params IButton[] buttons)
            {
                foreach (var b in buttons)
                    if (b.WasReleased) return true;
                return false;
            }
        }
    }

    //Class for representing a virtual input axis
    public class Axis
    {
        private string axis;

        public Axis(string axis)
        {
            this.axis = axis;
        }

        public float Value { get { return Input.GetAxisRaw(axis); } }

        public class Button : IButton
        {
            private const float AXIS_THRESHOLD = 0.5f;

            private Axis axis;
            private float oldValue;

            public Button(Axis axis)
            {
                this.axis = axis;
            }

            private void Update()
            {
                oldValue = axis.Value;
            }

            private bool Check(float v)
            {
                return Mathf.Abs(v) > AXIS_THRESHOLD;
            }

            public bool IsHeld { get { bool b = Check(axis.Value); Update(); return b; } }
            public bool WasPressed { get { bool b = Check(axis.Value) && !Check(oldValue); Update(); return b; } }
            public bool WasReleased { get { bool b = !Check(axis.Value) && Check(oldValue); Update(); return b; } }
        }
    }

    //Class to represent a 2d virtaul input axis (i.e. x and y axis)
    public class Axis2D
    {
        private Axis xAxis;
        private Axis yAxis;

        public Axis2D(string xAxis, string yAxis)
        {
            this.xAxis = new Axis(xAxis);
            this.yAxis = new Axis(yAxis);
        }

        public float X { get { return xAxis.Value; } }
        public float Y { get { return yAxis.Value; } }
        public Axis AxisX { get { return xAxis; } }
        public Axis AxisY { get { return yAxis; } }
    }
	
    //Class to represent an analog stick (i.e. 2d Axis + Button)
    public class AnalogStick
    {
        protected const float STICK_DEAD_ZONE = 0.15f;

        private Axis2D axis;
        private Button press;

        public AnalogStick(string x, string y, string press)
        {
            axis = new Axis2D(x, y);
            this.press = new Button(press);
        }

        public float X { get { return DeadZone(axis.X); } }
        public float Y { get { return DeadZone(axis.Y * -1); } }

        private float DeadZone(float value) { return Mathf.Abs(value) < STICK_DEAD_ZONE ? 0 : value; }

        public bool IsHeld { get { return press.IsHeld; } }
        public bool WasPressed { get { return press.WasPressed; } }
        public bool WasReleased { get { return press.WasReleased; } }
        public Axis AxisX { get { return axis.AxisX; } }
        public Axis AxisY { get { return axis.AxisY; } }
        public Axis2D AxisXY { get { return axis; } }
    }

    //Class that represents a full controller/gamepad (uses XBOX One controller mapping)
    //TODO: Allow for different mappings
    public class Controller
    {
        //Controller thresholds
        protected const float STICK_THRESHOLD = 0.5f;

        //Controller axes names defined in input manager
        protected const string LEFT_ANALOG_X = "LX";
        protected const string LEFT_ANALOG_Y = "LY";
        protected const string RIGHT_ANALOG_X = "RX";
        protected const string RIGHT_ANALOG_Y = "RY";
        protected const string DPAD_X = "DX";
        protected const string DPAD_Y = "DY";
        protected const string LEFT_TRIGGER = "LT";
        protected const string RIGHT_TRIGGER = "RT";

        //Controller button codes
        protected const int A_BUTTON = 0;
        protected const int B_BUTTON = 1;
        protected const int X_BUTTON = 2;
        protected const int Y_BUTTON = 3;
        protected const int START_BUTTON = 7;
        protected const int BACK_BUTTON = 6;
        protected const int LEFT_STICK_BUTTON = 8;
        protected const int RIGHT_STICK_BUTTON = 9;
        protected const int LEFT_BUMPER_BUTTON = 4;
        protected const int RIGHT_BUMPER_BUTTON = 5;

        //Controller components
        public AnalogStick LeftStick, RightStick;
        public Axis2D DPad;
        public Axis LeftTrigger, RightTrigger;
        public IButton A, B, X, Y, Start, Back, LeftBumper, RightBumper;
        public AnyButton AnyKey;
        public Player Player { get; private set; }

        //Helper components
        public IButton Left, Right, Up, Down;

        //Static property for Controller 1-4
        private static Controller controllerOne;
        public static Controller One { get { return GetController(ref controllerOne, Player.One); } }
        private static Controller controllerTwo;
        public static Controller Two { get { return GetController(ref controllerTwo, Player.Two); } }
        private static Controller controllerThree;
        public static Controller Three { get { return GetController(ref controllerThree, Player.Three); } }
        private static Controller controllerFour;
        public static Controller Four { get { return GetController(ref controllerFour, Player.Four); } }
        
        public Controller(Player player)
        {
            //Rember the "Player" of the controller (index)
            this.Player = player;

            //Configure components
            LeftStick = new AnalogStick(
                GetAxisString(LEFT_ANALOG_X, player),
                GetAxisString(LEFT_ANALOG_Y, player), 
                GetButtonString(LEFT_STICK_BUTTON, player));
            RightStick = new AnalogStick(
                GetAxisString(RIGHT_ANALOG_X, player),
                GetAxisString(RIGHT_ANALOG_Y, player),
                GetButtonString(RIGHT_STICK_BUTTON, player));
            DPad = new Axis2D(
                GetAxisString(DPAD_X, player),
                GetAxisString(DPAD_Y, player)
                );
            LeftTrigger = new Axis(
                GetAxisString(LEFT_TRIGGER, player)
                );
            RightTrigger = new Axis(
                GetAxisString(RIGHT_TRIGGER, player)
                );

            A = new Button(
                GetButtonString(A_BUTTON, player)
                );
            B = new Button(
                GetButtonString(B_BUTTON, player)
                );
            X = new Button(
                GetButtonString(X_BUTTON, player)
                );
            Y = new Button(
                GetButtonString(Y_BUTTON, player)
                );
            Start = new Button(
                GetButtonString(START_BUTTON, player)
                );
            Back = new Button(
                GetButtonString(BACK_BUTTON, player)
                );
            LeftBumper = new Button(
                GetButtonString(LEFT_BUMPER_BUTTON, player)
                );
            RightBumper = new Button(
                GetButtonString(RIGHT_BUMPER_BUTTON, player)
                );

            Left = new DirectionalButton(LeftStick.AxisX, DPad.AxisX, true);
            Right = new DirectionalButton(LeftStick.AxisX, DPad.AxisX, false);
            Up = new DirectionalButton(LeftStick.AxisY, DPad.AxisY, false);
            Down = new DirectionalButton(LeftStick.AxisY, DPad.AxisY, true);

            AnyKey = new Controller.AnyButton(A, B, X, Y, Start, Back, LeftBumper, RightBumper);
        }

        //Generate the axis string from the axis name and the player index
        private static string GetAxisString(string axis, Player player)
        {
            return string.Format("P{0}_{1}", (int)player, axis);
        }

        //Generate the button/keycode name from the button id and the player index
        private static string GetButtonString(int button, Player player)
        {
            return string.Format("Joystick{0}Button{1}", (int)player, button);
        }

        //Check if the controller is null and create one for the coresponding player if necessery
        private static Controller GetController(ref Controller controller, Player player)
        {
            if (controller == null) controller = new Controller(player);
            return controller;
        }

        //Get the controller that has pressed down any button in this frame
        public static Controller GetPressed()
        {
            if (One.AnyKey.WasPressed) return One;
            if (Two.AnyKey.WasPressed) return Two;
            if (Three.AnyKey.WasPressed) return Three;
            if (Four.AnyKey.WasPressed) return Four;
            return null;
        }

        //A virtual button that responds to multiple buttons
        public class AnyButton : IButton
        {
            private IButton[] buttons;

            public AnyButton(params IButton[] buttons)
            {
                this.buttons = buttons;
            }

            public bool IsHeld { get { return Button.Any.IsHeld(buttons); } }
            public bool WasPressed { get { return Button.Any.WasPressed(buttons); } }
            public bool WasReleased { get { return Button.Any.WasReleased(buttons); } }

        }

        public class DirectionalButton : IButton
        {
            private Axis axis1, axis2;
            private float oldValue1, oldValue2;
            private bool negative;

            public DirectionalButton(Axis a1, Axis a2, bool negative)
            {
                this.negative = negative;
                axis1 = a1;
                axis2 = a2;
            }

            private void Update()
            {
                oldValue1 = axis1.Value;
                oldValue2 = axis2.Value;
            }

            private bool Check(float value) { return negative ? value > STICK_THRESHOLD : value < -STICK_THRESHOLD; }
            private bool Check(Axis axis) { return Check(axis.Value); }

            public bool IsHeld { get { bool v = Check(axis1) || Check(axis2); Update(); return v; } }
            public bool WasPressed { get { bool v = (Check(axis1) && !Check(oldValue1)) || (Check(axis2) && !Check(oldValue2)); Update(); return v; } }
            public bool WasReleased { get { bool v = (!Check(axis1) && Check(oldValue1)) || (!Check(axis2) && Check(oldValue2)); Update(); return v; } }
        }

        //Controller that responds to button inputs from player 1-4
        private static Controller any;
        public static Controller Any
        {
            get {
                if (any == null)
                {
                    any = new Controller(Player.One);
                    any.A = new AnyButton(One.A, Two.A, Three.A, Four.A);
                    any.B = new AnyButton(One.B, Two.B, Three.B, Four.B);
                    any.X = new AnyButton(One.X, Two.X, Three.X, Four.X);
                    any.Y = new AnyButton(One.Y, Two.Y, Three.Y, Four.Y);
                    any.LeftBumper = new AnyButton(One.LeftBumper, Two.LeftBumper, Three.LeftBumper, Four.LeftBumper);
                    any.RightBumper = new AnyButton(One.RightBumper, Two.RightBumper, Three.RightBumper, Four.RightBumper);
                    any.Start = new AnyButton(One.Start, Two.Start, Three.Start, Four.Start);
                    any.Back = new AnyButton(One.Back, Two.Back, Three.Back, Four.Back);
                    any.AnyKey = new AnyButton(any.A, any.B, any.X, any.Y, any.LeftBumper, any.RightBumper, any.Start, any.Back);
                    any.Left = new AnyButton(One.Left, Two.Left, Three.Left, Four.Left);
                    any.Right = new AnyButton(One.Right, Two.Right, Three.Right, Four.Right);
                    any.Up = new AnyButton(One.Up, Two.Up, Three.Up, Four.Up);
                    any.Down = new AnyButton(One.Down, Two.Down, Three.Down, Four.Down);
                }
                return any;
            }
        }
    }

    //Possible controllers
    public enum Player { One = 1, Two = 2, Three = 3, Four = 4 }
}
