﻿using GHI.Athens.Gadgeteer;
using GHI.Athens.Gadgeteer.SocketInterfaces;
using System;
using System.Threading.Tasks;

namespace GHI.Athens.Modules {
	public class CharacterDisplay : Module {
		private static uint[] RowOffsets { get; } = new uint[4] { 0x00, 0x40, 0x14, 0x54 };
		private static uint DisplayOnCommand { get; } = 0x0C;
		private static uint ClearDisplayCommand { get; } = 0x01;
		private static uint CursorHomeCommand { get; } = 0x02;
		private static uint SetCursorCommand { get; } = 0x80;

		private DigitalOutput lcdRS;
		private DigitalOutput lcdE;
		private DigitalOutput lcdD4;
		private DigitalOutput lcdD5;
		private DigitalOutput lcdD6;
		private DigitalOutput lcdD7;
		private DigitalOutput backlight;

		private uint currentRow;

		public override string Name { get; } = "CharacterDisplay";
		public override string Manufacturer { get; } = "GHI Electronics, LLC";

		protected async override Task Initialize(Socket parentSocket) {
			this.lcdRS = await parentSocket.CreateDigitalOutputAsync(SocketPinNumber.Four, false);
			this.lcdE = await parentSocket.CreateDigitalOutputAsync(SocketPinNumber.Three, false);
			this.lcdD4 = await parentSocket.CreateDigitalOutputAsync(SocketPinNumber.Five, false);
			this.lcdD5 = await parentSocket.CreateDigitalOutputAsync(SocketPinNumber.Seven, false);
			this.lcdD6 = await parentSocket.CreateDigitalOutputAsync(SocketPinNumber.Nine, false);
			this.lcdD7 = await parentSocket.CreateDigitalOutputAsync(SocketPinNumber.Six, false);
			this.backlight = await parentSocket.CreateDigitalOutputAsync(SocketPinNumber.Eight, true);

			this.currentRow = 0;

			this.SendCommand(0x33);
			this.SendCommand(0x32);
			this.SendCommand(CharacterDisplay.DisplayOnCommand);
			this.SendCommand(CharacterDisplay.ClearDisplayCommand);

			Task.Delay(3).Wait();
		}

		public bool BacklightEnabled {
			get {
				return this.backlight.Read();
			}
			set {
				this.backlight.Write(value);
			}
		}

		private void WriteNibble(byte b) {
			this.lcdD7.Write((b & 0x8) != 0);
			this.lcdD6.Write((b & 0x4) != 0);
			this.lcdD5.Write((b & 0x2) != 0);
			this.lcdD4.Write((b & 0x1) != 0);

			this.lcdE.SetHigh();
			this.lcdE.SetLow();

			Task.Delay(1).Wait();
		}

		private void SendCommand(uint command) {
			this.lcdRS.SetLow();

			this.WriteNibble((byte)(command >> 4));
			this.WriteNibble((byte)command);

			Task.Delay(2).Wait();

			this.lcdRS.SetHigh();
		}

		public void Print(string value) {
			for (int i = 0; i < value.Length; i++)
				this.Print(value[i]);
		}

		public void Print(char value) {
			if (value != '\n') {
				this.WriteNibble((byte)(value >> 4));
				this.WriteNibble((byte)value);
			}
			else {
				this.SetCursorPosition((this.currentRow + 1) % 2, 0);
			}
		}

		public void Clear() {
			this.SendCommand(CharacterDisplay.ClearDisplayCommand);

			this.currentRow = 0;

			Task.Delay(2).Wait();
		}

		public void CursorHome() {
			this.SendCommand(CharacterDisplay.CursorHomeCommand);

			this.currentRow = 0;

			Task.Delay(2).Wait();
		}

		public void SetCursorPosition(uint row, uint column) {
			if (column > 15) throw new ArgumentOutOfRangeException(nameof(column), $"{nameof(column)} must be between 0 and 15.");
			if (row > 1) throw new ArgumentOutOfRangeException(nameof(row), $"{nameof(row)} must be between 0 and 1.");

			this.currentRow = row;

            this.SendCommand((byte)(CharacterDisplay.SetCursorCommand | CharacterDisplay.RowOffsets[row] | column));
		}
	}
}