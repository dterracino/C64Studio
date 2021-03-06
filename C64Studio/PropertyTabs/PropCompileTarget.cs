﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace C64Studio
{
  public partial class PropCompileTarget : PropertyTabs.PropertyTabBase
  {
    public class DependencyItem
    {
      public FileDependency.DependencyInfo    DependencyInfo = new FileDependency.DependencyInfo( "", false, false );
      public bool                             CanIncludeSymbols = false;


      public DependencyItem( FileDependency.DependencyInfo Info, bool CanIncludeSymbols )
      {
        DependencyInfo = Info;
        this.CanIncludeSymbols = CanIncludeSymbols;
      }
    }



    ProjectElement        Element;
    StudioCore            Core;

    public PropCompileTarget( ProjectElement Element, StudioCore Core )
    {
      this.Element = Element;
      this.Core = Core;
      TopLevel = false;
      Text = "Compile Target";
      InitializeComponent();

      comboTargetType.Items.Add( "None" );
      comboTargetType.Items.Add( "Plain" );
      comboTargetType.Items.Add( "PRG (cbm)" );
      comboTargetType.Items.Add( "T64" );
      comboTargetType.Items.Add( "8 KB Cartridge (bin)" );
      comboTargetType.Items.Add( "8 KB Cartridge (crt)" );
      comboTargetType.Items.Add( "16 KB Cartridge (bin)" );
      comboTargetType.Items.Add( "16 KB Cartridge (crt)" );
      comboTargetType.Items.Add( "D64" );
      comboTargetType.Items.Add( "Magic Desk 64 KB Cartridge (bin)" );
      comboTargetType.Items.Add( "Magic Desk 64 KB Cartridge (crt)" );
      comboTargetType.Items.Add( "TAP" );
      comboTargetType.Items.Add( "Easyflash Cartridge (bin)" );
      comboTargetType.Items.Add( "Easyflash Cartridge (crt)" );
      comboTargetType.Items.Add( "RGCD 64 KB Cartridge (bin)" );
      comboTargetType.Items.Add( "RGCD 64 KB Cartridge (crt)" );

      comboTargetType.SelectedIndex = (int)Element.TargetType;

      editTargetFilename.Text = Element.TargetFilename;

      foreach ( ProjectElement element in Element.DocumentInfo.Project.Elements )
      {
        if ( ( element != Element )
        &&   ( element.DocumentInfo.Type != ProjectElement.ElementType.FOLDER ) )
        {
          var dependencies = Element.DocumentInfo.Project.GetDependencies( element );

          FileDependency.DependencyInfo   depInfo = Element.ForcedDependency.FindDependency( element.Filename );
          if ( depInfo == null )
          {
            depInfo = new FileDependency.DependencyInfo( element.Filename, false, false );
          }

          bool isDependent = false;
          foreach ( var dependency in dependencies )
          {
            if ( dependency.Filename == Element.Filename )
            {
              isDependent = true;
              break;
            }
          }
          if ( !isDependent )
          {
            // not itself!
            DependencyItem depItem = new DependencyItem( depInfo, element.DocumentInfo.Type == ProjectElement.ElementType.ASM_SOURCE );


            ListViewItem    item = new ListViewItem( element.Filename );

            if ( Element.ForcedDependency.DependsOn( element.Filename ) )
            {
              item.SubItems.Add( "1" );
            }
            else
            {
              item.SubItems.Add( "0" );
            }
            if ( depInfo.IncludeSymbols )
            {
              item.SubItems.Add( "1" );
            }
            else
            {
              item.SubItems.Add( "0" );
            }
            item.Tag = depItem;
            listDependencies.Items.Add( item );
          }
        }
      }
    }



    private void btnParseTarget_Click( object sender, EventArgs e )
    {
      Core.MainForm.EnsureFileIsParsed();

      if ( Element.CompileTargetFile == null )
      {
        editTargetFilename.Text = "";
      }
      else
      {
        string relativeFilename = GR.Path.RelativePathTo( Element.CompileTargetFile, false, System.IO.Path.GetFullPath( Element.DocumentInfo.Project.Settings.BasePath ), true );
        editTargetFilename.Text = relativeFilename;
      }
      comboTargetType.SelectedIndex = (int)Element.CompileTarget;
    }



    public override void OnClose()
    {
      Element.TargetFilename = editTargetFilename.Text;
      Element.TargetType = (Types.CompileTargetType)comboTargetType.SelectedIndex;

      // rebuild dependency list
      Element.ForcedDependency.DependentOnFile.Clear();
      foreach ( ListViewItem item in listDependencies.Items )
      {
        DependencyItem depItem = (DependencyItem)item.Tag;

        if ( depItem.DependencyInfo.Dependent )
        {
          Element.ForcedDependency.DependentOnFile.Add( depItem.DependencyInfo );
        }
      }
    }



    private void listDependencies_DrawSubItem( object sender, DrawListViewSubItemEventArgs e )
    {
      if ( ( e.Item == null )
      ||   ( e.ColumnIndex == 0 ) )
      {
        e.DrawDefault = true;
        return;
      }
      e.DrawBackground();
      DependencyItem    depItem = (DependencyItem)e.Item.Tag;
      if ( e.ColumnIndex == 1 )
      {
        System.Windows.Forms.ControlPaint.DrawCheckBox( e.Graphics, e.SubItem.Bounds, depItem.DependencyInfo.Dependent ? ButtonState.Checked : ButtonState.Normal );
      }
      else if ( e.ColumnIndex == 2 )
      {
        if ( depItem.CanIncludeSymbols )
        {
          System.Windows.Forms.ControlPaint.DrawCheckBox( e.Graphics, e.SubItem.Bounds, depItem.DependencyInfo.IncludeSymbols ? ButtonState.Checked : ButtonState.Normal );
        }
      }
    }



    private void listDependencies_DrawColumnHeader( object sender, DrawListViewColumnHeaderEventArgs e )
    {
      e.DrawDefault = true;
    }



    private void listDependencies_DrawItem( object sender, DrawListViewItemEventArgs e )
    {
      e.DrawBackground();
      e.DrawText();
      e.DrawFocusRectangle();
    }



    private void listDependencies_MouseDown( object sender, MouseEventArgs e )
    {
      var hitInfo = listDependencies.HitTest( e.Location );
      if ( hitInfo.Item != null )
      {
        DependencyItem    depItem = (DependencyItem)hitInfo.Item.Tag;

        if ( hitInfo.SubItem == hitInfo.Item.SubItems[1] )
        {
          // dependent
          depItem.DependencyInfo.Dependent = !depItem.DependencyInfo.Dependent;
          if ( !depItem.DependencyInfo.Dependent )
          {
            depItem.DependencyInfo.IncludeSymbols = false;
          }
          Element.DocumentInfo.Project.SetModified();
          listDependencies.Invalidate( hitInfo.Item.Bounds );
        }
        else if ( hitInfo.SubItem == hitInfo.Item.SubItems[2] )
        {
          // include symbols
          if ( ( depItem.DependencyInfo.Dependent )
          &&   ( depItem.CanIncludeSymbols ) )
          {
            depItem.DependencyInfo.IncludeSymbols = !depItem.DependencyInfo.IncludeSymbols;
            Element.DocumentInfo.Project.SetModified();
            listDependencies.Invalidate( hitInfo.Item.Bounds );
          }
        }
      }
    }



    private void editTargetFilename_TextChanged( object sender, EventArgs e )
    {
      if ( editTargetFilename.Text != Element.TargetFilename )
      {
        Element.DocumentInfo.Project.SetModified();
      }
    }



    private void comboTargetType_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( comboTargetType.SelectedIndex != (int)Element.TargetType )
      {
        Element.DocumentInfo.Project.SetModified();
      }
    }

  }
}
