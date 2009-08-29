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
using s3pi.Interfaces;
using System.Reflection;

namespace System.Windows.Forms.TGIBlockListEditorForm
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            tbGroup.Text = tbInstance.Text = "";
            btnAdd.Enabled = items != null && (items.MaxSize == -1 || listView1.Items.Count < items.MaxSize);
            btnDelete.Enabled = listView1.SelectedItems.Count > 0;
        }

        AResource.TGIBlockList items;
        public IList<AResource.TGIBlock> Items
        {
            get { return items; }
            set
            {
                if (items == value) return;
                items = new AResource.TGIBlockList(null, value);

                listView1.Items.Clear();

                foreach (AResource.TGIBlock tgib in items)
                {
                    ListViewItem lvi = CreateListViewItem(tgib);
                    lvi.Tag = tgib;
                    listView1.Items.Add(lvi);
                }
                if (items.Count > 0)
                    listView1.Items[0].Selected = true;
                btnAdd.Enabled = items.MaxSize == -1 || listView1.Items.Count < items.MaxSize;
                btnDelete.Enabled = listView1.SelectedItems.Count > 0;
            }
        }
        private ListViewItem CreateListViewItem(AResource.TGIBlock tgib)
        {
            ListViewItem lvi = new ListViewItem();
            lvi.Text = s3pi.Extensions.ExtList.Ext.ContainsKey("0x" + tgib.ResourceType.ToString("X8"))
                ? s3pi.Extensions.ExtList.Ext["0x" + tgib.ResourceType.ToString("X8")][0] : "";
            lvi.SubItems.AddRange(new string[] {
                "0x" + tgib.ResourceType.ToString("X8"),
                "0x" + tgib.ResourceGroup.ToString("X8"),
                "0x" + tgib.Instance.ToString("X16"),
            });
            return lvi;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            items.Add((uint)0, (uint)0, (ulong)0);
            ListViewItem lvi = CreateListViewItem(items[items.Count - 1]);
            lvi.Tag = items[items.Count - 1];
            listView1.Items.Add(lvi);
            btnAdd.Enabled = items.MaxSize == -1 || listView1.Items.Count < items.MaxSize;
            lvi.Selected = true;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            int i = listView1.SelectedIndices[0];
            listView1.SelectedIndices.Clear();
            items.RemoveAt(i);
            listView1.Items.RemoveAt(i);
            i--;
            if (i < 0 && items.Count > 0) i = 0;
            if (i >= 0)
                listView1.Items[i].Selected = true;
            btnDelete.Enabled = listView1.SelectedItems.Count > 0;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 0)
            {
                cbType.Enabled = tbGroup.Enabled = tbInstance.Enabled = false;
                cbType.Text = tbGroup.Text = tbInstance.Text = "";
            }
            else
            {
                cbType.Enabled = tbGroup.Enabled = tbInstance.Enabled = true;
                AResource.TGIBlock item = listView1.SelectedItems[0].Tag as AResource.TGIBlock;
                cbType.Value = item.ResourceType;
                tbGroup.Text = "0x" + item.ResourceGroup.ToString("X8");
                tbInstance.Text = "0x" + item.Instance.ToString("X16");
            }
            btnDelete.Enabled = listView1.SelectedItems.Count > 0;
        }

        private void cbType_ValueChanged(object sender, EventArgs e)
        {
            if (cbType.Valid) cbType_Validated(sender, e);
        }

        private void cbType_Validating(object sender, CancelEventArgs e)
        {
            e.Cancel = !cbType.Valid;
        }

        private void cbType_Validated(object sender, EventArgs e)
        {
            items[listView1.SelectedIndices[0]].ResourceType = cbType.Value;
            ListViewItem lvi = CreateListViewItem(items[listView1.SelectedIndices[0]]);
            listView1.SelectedItems[0].Text = lvi.Text;
            listView1.SelectedItems[0].SubItems[1].Text = lvi.SubItems[1].Text;
        }

        private void tbGroup_Validating(object sender, CancelEventArgs e)
        {
            uint res;
            string s = tbGroup.Text.Trim().ToLower();
            if (s.StartsWith("0x"))
                e.Cancel = !uint.TryParse(s.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out res);
            else
                e.Cancel = !uint.TryParse(s, out res);
            if (e.Cancel) tbGroup.SelectAll();
        }

        private void tbGroup_Validated(object sender, EventArgs e)
        {
            uint res;
            string s = tbGroup.Text.Trim().ToLower();
            if (s.StartsWith("0x"))
                res = uint.Parse(s.Substring(2), System.Globalization.NumberStyles.HexNumber, null);
            else
                res = uint.Parse(s);

            items[listView1.SelectedIndices[0]].ResourceGroup = res;
            ListViewItem lvi = CreateListViewItem(items[listView1.SelectedIndices[0]]);
            listView1.SelectedItems[0].SubItems[2].Text = lvi.SubItems[2].Text;
        }

        private void tbInstance_Validating(object sender, CancelEventArgs e)
        {
            ulong res;
            string s = tbInstance.Text.Trim().ToLower();
            if (s.StartsWith("0x"))
                e.Cancel = !ulong.TryParse(s.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out res);
            else
                e.Cancel = !ulong.TryParse(s, out res);
            if (e.Cancel) tbInstance.SelectAll();
        }

        private void tbInstance_Validated(object sender, EventArgs e)
        {
            ulong res;
            string s = tbInstance.Text.Trim().ToLower();
            if (s.StartsWith("0x"))
                res = ulong.Parse(s.Substring(2), System.Globalization.NumberStyles.HexNumber, null);
            else
                res = ulong.Parse(s);

            items[listView1.SelectedIndices[0]].Instance = res;
            ListViewItem lvi = CreateListViewItem(items[listView1.SelectedIndices[0]]);
            listView1.SelectedItems[0].SubItems[3].Text = lvi.SubItems[3].Text;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

namespace System.Windows.Forms
{
    public static class TGIBlockListEditor
    {
        public static DialogResult Show(AResource.DependentList<AResource.TGIBlock> ltgi)
        {
            return Show(Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null, ltgi);
        }
        public static DialogResult Show(IWin32Window owner, AResource.DependentList<AResource.TGIBlock> ltgi)
        {
            TGIBlockListEditorForm.MainForm theForm = new System.Windows.Forms.TGIBlockListEditorForm.MainForm();
            theForm.Items = ltgi;
            if (owner as Form != null) theForm.Icon = (owner as Form).Icon;
            DialogResult dr = theForm.ShowDialog();
            if (dr != DialogResult.OK) return dr;
            ltgi.Clear();
            ltgi.AddRange(theForm.Items);
            return dr;
        }
    }
}