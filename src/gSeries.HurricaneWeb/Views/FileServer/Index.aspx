<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<dynamic>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    FileServer
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <h2>
        FileServer</h2>
    <p>
        File server that supports <a href="http://en.wikipedia.org/wiki/Byte_serving">"Byte
            Range Serving"</a>.
    </p>
</asp:Content>
