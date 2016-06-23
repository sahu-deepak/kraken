/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2011 Thomas Carpe. <http://www.ThomasCarpe.com/>
  Maintained by: <http://www.LiquidMercurySolutions.com/>

  This file is part of SPARK: SharePoint Application Resource Kit.
  SPARK projects are distributed via CodePlex: <http://www.codeplex.com/spark/>

  You may use this code for commercial purposes and derivative works, 
  provided that you maintain all copyright notices.

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version. You should have received a copy of
  the GNU General Public License along with SPARK.  If not, see
  <http://www.gnu.org/licenses/>.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  
  We worked hard on all SPARK code, and we don't make any profit from
  sharing it with the world. Please do us a favor amd give us credit
  where credit is due, by leaving this notice unchanged. We all stand
  on the backs of giants. Wherever we have used someone else's code or
  blog article as the basis of our work, we have provided references
  to our source.
*/

namespace Kraken.SharePoint.WebParts.ToolParts {

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Web.UI;

    using Microsoft.SharePoint;
    using Microsoft.SharePoint.WebControls;
    using Microsoft.SharePoint.WebPartPages;

    using Kraken.SharePoint.WebParts;

  /// <summary>
  /// This tool part works only in SharePoint Server (formerly MOSS) and was basically an improved version of:
  /// <seealso cref="http://darrenjohnstone.net/2008/01/23/sharepoint-picker-toolpart/" />
  /// </summary>
  /// <example>
  ///  // this property and method would go in the web part
  /// 
  ///  [Browsable(false), Category("Miscellaneous"), DefaultValue(""),WebPartStorage(Storage.Shared)]
  ///  public string TargetURL {
  ///    get; set;
  ///  }
  /// 
  ///  public override ToolPart[] GetToolParts() {
  ///    List&lt;ToolPart&gt; parts = new List&lt;ToolPart&gt;();
  ///    SiteAndListPickerToolPart picker = new SiteAndListPickerToolPart();
  ///
  ///    parts.AddRange(base.GetToolParts());
  ///
  ///    picker.Items.Add(new SiteAndListPickerItem("Site to query", "TargetURL", SiteAndListPickerType.Site));
  ///    parts.Add(picker);
  ///
  ///    return parts.ToArray();
  ///  }
  /// </example>
  public class SiteAndListPickerToolPart : ToolPart, INamingContainer {

    public List<SiteAndListPickerItem> Items { get; set; }

    /// <summary>
    /// When true, starts the picker in the root of the site collection.
    /// Otherwise, starts the picker in the current web site.
    /// </summary>
    public bool UseRootSite { get; set; }

    private Guid instance;
    private string instanceID;

    /// <summary> 
    /// Initializes a new instance of the <see cref="SiteAndListPickerToolPart"/> class. 
    /// </summary> 
    public SiteAndListPickerToolPart() : base() {
      Items = new List<SiteAndListPickerItem>();
      this.Title = "Target Data Source";
      this.UseRootSite = true;
      //this.WebPartPropertyName = "TargetURL";
      instance = Guid.NewGuid();
      instanceID = "Picker_" + instance.ToString().Replace("{", string.Empty).Replace("}", string.Empty);
    }

    /// <summary> 
    /// Registers the script to load the web selector. 
    /// </summary> 
    void RegisterPopupScript() {
      string url = this.UseRootSite 
        ? SPContext.Current.Site.RootWeb.ServerRelativeUrl 
        : SPContext.Current.Web.ServerRelativeUrl;
      StringBuilder sb = new StringBuilder();
      sb.Append("function cust_launchPickerSite(inputID)\r\n {\r\n  if (!document.getElementById) return;");
      sb.Append("var targetTextBox = document.getElementById(");
      sb.Append("inputID");
      sb.Append(");");
      sb.Append("if (targetTextBox == null) return;");
      sb.Append("var serverUrl = '");
      sb.Append(url);
      sb.Append("';var callbackSite = function(results){if (results == null || results[1] == null) return;targetTextBox.value = results[1];};\r\n");
      sb.Append("LaunchPickerTreeDialog('CbqPickerSelectSiteTitle','CbqPickerSelectSiteText','websOnly','',serverUrl, '','','','/_layouts/images/smt_icon.gif','', callbackSite);}");
      this.Page.ClientScript.RegisterClientScriptBlock(typeof(SiteAndListPickerToolPart), "Site" + instanceID, sb.ToString(), true);

      sb = new StringBuilder();
      sb.Append("function cust_launchPickerList(inputID)\r\n {\r\n  if (!document.getElementById) return;");
      sb.Append("var targetTextBox = document.getElementById(");
      sb.Append("inputID");
      sb.Append(");");
      sb.Append("if (targetTextBox == null) return;");
      sb.Append("var serverUrl = '");
      sb.Append(url);
      sb.Append("';var callbackList = function(results){if (results == null || results[1] == null || results[2] == null) return;targetTextBox.value = results[1]+(results[1]=='/' ? '' : '/')+results[2];};\r\n");
      sb.Append("LaunchPickerTreeDialog('CbqPickerSelectListTitle','CbqPickerSelectListText','listsOnly','',serverUrl, '','','','/_layouts/images/smt_icon.gif','', callbackList);}");
      this.Page.ClientScript.RegisterClientScriptBlock(typeof(SiteAndListPickerToolPart), "List" + instanceID, sb.ToString(), true);

      this.Page.ClientScript.RegisterClientScriptInclude("PickerTreeDialog", "/_layouts/1033/PickerTreeDialog.js");
    }

    /// <summary> 
    /// Raises the <see cref="E:System.Web.UI.Control.Load"/> event. 
    /// </summary> 
    /// <param name="e">The <see cref="T:System.EventArgs"/> object that contains the event data.</param> 
    protected override void OnLoad(EventArgs e) {
      base.OnLoad(e);
      RegisterPopupScript();
    }

    /// <summary> 
    /// Sends the tool part content to the specified HtmlTextWriter object, which writes the content to be rendered on the client. 
    /// </summary> 
    /// <param name="output">The HtmlTextWriter object that receives the tool part content.</param> 
    protected override void RenderToolPart(HtmlTextWriter output) {
      WebPart parent;
      int i = 0;
      parent = this.ParentToolPane.SelectedWebPart;
      if (Items.Count > 0) {
        output.Write("<table cellspacing=\"0\" border=\"0\" style=\"border-width:0px;width:100%;border-collapse:collapse;\">");
        foreach (SiteAndListPickerItem pi in Items) {
          output.Write("<tr><td>");
          output.Write("<div class=\"UserSectionHead\">");
          output.Write(pi.Title);
          output.Write("</div>");
          output.Write("<div class=\"UserControlGroup\"><nobr>");
          output.Write("<input type=\"text\" ");
          output.Write("value=\"" + parent.GetWebPartProperty(pi.PropertyName) + "\"");
          output.Write(" name=\"picker_" + i.ToString() + "\" id=\"picker_" + i.ToString() + "\"/>");
          output.Write("<input type=\"button\" onclick=\"" + (pi.PickerType == SiteAndListPickerType.Site ? "cust_launchPickerSite" : "cust_launchPickerList"));
          output.Write("('picker_" + i.ToString() + "'); return false;\" value=\"...\"/>");
          output.Write("</nobr></div>");
          output.Write("</td></tr>");
          i++;
        }
        output.Write("</table>");
      }
    }

    /// <summary> 
    /// Called when the user clicks the OK or the Apply button in the tool pane. 
    /// </summary> 
    public override void ApplyChanges() {
      WebPart parent;
      int i = 0;
      parent = ParentToolPane.SelectedWebPart;
      if (Items.Count > 0) {
        foreach (SiteAndListPickerItem pi in Items) {
          parent.SetWebPartProperty(pi.PropertyName, Page.Request.Form["picker_" + i.ToString()]);
          i++;
        }
      }
    }

  } // class

  public enum SiteAndListPickerType {
    Site,
    List
  }

  public class SiteAndListPickerItem {

    public string Title { private set;  get;  }
    public string PropertyName { private set; get; }
    public SiteAndListPickerType PickerType { private set; get; } 

    public SiteAndListPickerItem(string title, string propertyName, SiteAndListPickerType pickerType) {
      Title = title;
      PropertyName = propertyName;
      PickerType = pickerType;
    }

  } // PickerItem

} // namespace