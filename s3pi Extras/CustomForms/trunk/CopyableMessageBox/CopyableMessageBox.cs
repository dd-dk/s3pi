﻿/***************************************************************************
 *  Copyright (C) 2009 by Peter L Jones                                    *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace System.Windows.Forms
{
    internal partial class CopyableMessageBoxInternal : Form
    {
        internal Button theButton = null;
        private CopyableMessageBoxInternal()
        {
            InitializeComponent();
        }

        Label lb = new Label();
        internal CopyableMessageBoxInternal(string message, string caption, CopyableMessageBoxIcon icon, IList<string> buttons, int defBtn, int cncBtn)
            : this()
        {
            if (buttons.Count < 1)
                throw new ArgumentLengthException("At least one button text must be supplied");

            this.SuspendLayout();


            int formWidth = Bounds.Width - ClientSize.Width; // screen estate used by the form border
            int formHeight = Bounds.Height - ClientSize.Height; // screen estate used by the form border
            int tbPadding = tbMessage.Margin.Left + tbMessage.Margin.Right; // screen estate used by the text box regardless of content
            int buttonHeight = flpButtons.Height; // screen estate reserved for the buttons

            int iconWidth = icon == CopyableMessageBoxIcon.None ? 0 : 80; // icon area, if icon present
            int iconHeight = icon == CopyableMessageBoxIcon.None ? 0 : 77; // icon area, if icon present

            // To calculate the text box size, we get an autosize label to tell us how big it should be
            Size winSize = Application.OpenForms.Count > 0 ? Application.OpenForms[0].Size : Screen.PrimaryScreen.WorkingArea.Size;
            lb.MaximumSize = new Size((int)(winSize.Width * .8) - (formWidth + tbPadding + iconWidth),
                (int)(winSize.Height * .8) - (formHeight + buttonHeight + tbPadding));
            lb.AutoSize = true;
            lb.Text = message;

            tbMessage_SizeChanged(tbMessage, null);

            int buttonWidth = 15 + (81 * (buttons.Count - 1)) + 75 + 15;
            int textWidth = tbPadding + lb.PreferredWidth;
            int textHeight = tbPadding + lb.PreferredHeight;

            this.ClientSize = new Size(Math.Max(buttonWidth, iconWidth + textWidth),
                buttonHeight + Math.Max(iconHeight, textHeight));


            this.Text = caption;

            this.tbMessage.Lines = message.Split('\n');

            enumToGlyph(icon, lbIcon);
            
            CreateButtons(buttons, defBtn, cncBtn);


            this.ResumeLayout();

            this.DialogResult = DialogResult.OK;
        }

        private void enumToGlyph(CopyableMessageBoxIcon icon, Label lb)
        {
            switch (icon)
            {
                case CopyableMessageBoxIcon.Information:
                    lb.Visible = true; lb.Text = "i"; lb.ForeColor = Color.Blue; lb.BackColor = Color.FromArgb(240, 240, 255); lb.Font = new Font(lb.Font, FontStyle.Italic); break;
                case CopyableMessageBoxIcon.Question:
                    lb.Visible = true; lb.Text = "?"; lb.ForeColor = Color.Green; lb.BackColor = Color.FromArgb(240, 255, 240); lb.Font = new Font(lb.Font, FontStyle.Regular); break;
                case CopyableMessageBoxIcon.Warning:
                    lb.Visible = true; lb.Text = "!"; lb.ForeColor = Color.Black; lb.BackColor = Color.Yellow; lb.Font = new Font(lb.Font, FontStyle.Bold); break;
                case CopyableMessageBoxIcon.Error:
                    lb.Visible = true; lb.Text = "X"; lb.ForeColor = Color.White; lb.BackColor = Color.Red; lb.Font = new Font(lb.Font, FontStyle.Bold); break;
                case CopyableMessageBoxIcon.None:
                default:
                    lb.Visible = false; break;
            }
        }

        private void CreateButtons(IList<string> buttons, int defBtn, int cncBtn)
        {
            flpButtons.SuspendLayout();
            flpButtons.Controls.Clear();
            for (int i = buttons.Count; i > 0; i--)
            {
                Button btn = CreateButton("button" + i, i, buttons[i - 1]);
                flpButtons.Controls.Add(btn);
                if (i == defBtn + 1) this.AcceptButton = btn;
                if (i == cncBtn + 1) this.CancelButton = btn;
            }
            flpButtons.ResumeLayout();
        }

        private Button CreateButton(string Name, int TabIndex, string Text)
        {
            Button newButton = new Button();
            newButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            newButton.Name = Name;
            newButton.Size = new System.Drawing.Size(75, 23);
            newButton.TabIndex = TabIndex;
            newButton.Text = Text;
            newButton.UseVisualStyleBackColor = true;
            newButton.Click += new System.EventHandler(this.button_Click);
            return newButton;
        }

        private void button_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;//To avoid it becoming Cancel
            theButton = sender as Button;
            this.Close();
        }

        private void tbMessage_SizeChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            tbMessage.ScrollBars = ((tb.Height < lb.PreferredHeight || tb.Width < lb.PreferredWidth) ? ScrollBars.Vertical : ScrollBars.None);
        }
    }

    public enum CopyableMessageBoxIcon
    {
        None = 0,
        Information = 1,
        Asterisk = 1,
        Question = 2,
        Warning = 3,
        Exclamation = 3,
        Error = 4,
        Hand = 4,
        Stop = 4,
    }

    public enum CopyableMessageBoxButtons
    {
        OK,
        OKCancel,
        AbortRetryIgnore,
        YesNoCancel,
        YesNo,
        RetryCancel,
    }

    public static class CopyableMessageBox
    {
        public static int Show(string message)
        {
            return Show(message, Application.OpenForms.Count > 0 ? Application.OpenForms[0].Text : "",
                CopyableMessageBoxIcon.None, new List<string>(new string[] { "OK" }), 0, 0);
        }
        public static int Show(string message, string caption)
        {
            return Show(message, caption, CopyableMessageBoxIcon.None, new List<string>(new string[] { "OK" }), 0, 0);
        }
        public static int Show(string message, string caption, CopyableMessageBoxButtons buttons)
        {
            int cncBtn = enumToCncBtn(buttons);
            return Show(message, caption, CopyableMessageBoxIcon.None, enumToList(buttons), 0, cncBtn);
        }
        public static int Show(string message, string caption, CopyableMessageBoxButtons buttons, CopyableMessageBoxIcon icon)
        {
            int cncBtn = enumToCncBtn(buttons);
            return Show(message, caption, icon, enumToList(buttons), 0, cncBtn);
        }
        public static int Show(string message, string caption, CopyableMessageBoxButtons buttons, CopyableMessageBoxIcon icon, int defBtn)
        {
            int cncBtn = enumToCncBtn(buttons);
            return Show(message, caption, icon, enumToList(buttons), defBtn, cncBtn);
        }
        public static int Show(string message, string caption, CopyableMessageBoxButtons buttons, CopyableMessageBoxIcon icon, int defBtn, int cncBtn)
        {
            return Show(message, caption, icon, enumToList(buttons), defBtn, cncBtn);
        }

        public static int Show(string message, string caption, CopyableMessageBoxIcon icon, IList<string> buttons, int defBtn, int cncBtn)
        {
            return Show(Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null, message, caption, icon, buttons, defBtn, cncBtn);
        }

        public static int Show(IWin32Window owner, string message, string caption, CopyableMessageBoxIcon icon, IList<string> buttons, int defBtn, int cncBtn)
        {
            CopyableMessageBoxInternal cmb = new CopyableMessageBoxInternal(message, caption, icon, buttons, defBtn, cncBtn);
            if (owner != null) cmb.Icon = ((Form)owner).Icon;
            else owner = new Form();

            if (cmb.ShowDialog(owner) == DialogResult.Cancel) return cncBtn;
            return (cmb.theButton != null) ? buttons.IndexOf(cmb.theButton.Text) : -1;
        }

        private static int enumToCncBtn(CopyableMessageBoxButtons buttons)
        {
            switch (buttons)
            {
                case CopyableMessageBoxButtons.OK: return 0;
                case CopyableMessageBoxButtons.OKCancel: return 1;
                case CopyableMessageBoxButtons.RetryCancel: return 1;
                case CopyableMessageBoxButtons.AbortRetryIgnore: return -1;
                case CopyableMessageBoxButtons.YesNoCancel: return 2;
                case CopyableMessageBoxButtons.YesNo: return -1;
                default: return -1;
            }
        }

        private static IList<string> enumToList(CopyableMessageBoxButtons buttons)
        {
            switch (buttons)
            {
                case CopyableMessageBoxButtons.OKCancel: return new List<string>(new string[] { "&OK", "&Cancel", });
                case CopyableMessageBoxButtons.AbortRetryIgnore: return new List<string>(new string[] { "&Abort", "&Retry", "&Ignore", });
                case CopyableMessageBoxButtons.RetryCancel: return new List<string>(new string[] { "&Retry", "&Cancel", });
                case CopyableMessageBoxButtons.YesNoCancel: return new List<string>(new string[] { "&Yes", "&No", "&Cancel", });
                case CopyableMessageBoxButtons.YesNo: return new List<string>(new string[] { "&Yes", "&No", });
                case CopyableMessageBoxButtons.OK:
                default: return new List<string>(new string[] { "&OK", });
            }
        }

        public static void IssueException(Exception ex) { IssueException(ex, "", "Program Exception"); }
        public static void IssueException(Exception ex, string caption) { IssueException(ex, "", caption); }
        public static void IssueException(Exception ex, string prefix, string caption)
        {
            string s = prefix;
            for (Exception inex = ex; inex != null; inex = inex.InnerException)
            {
                s += "\nSource: " + inex.Source;
                s += "\nAssembly: " + inex.TargetSite.DeclaringType.Assembly.FullName;
                s += "\n" + inex.Message;
                s += "\n" + inex.StackTrace;
                s += "\n-----";
            }
            CopyableMessageBox.Show(s, caption, CopyableMessageBoxButtons.OK, CopyableMessageBoxIcon.Stop);
        }

    }
}
