<%@ Page Title="" Language="C#" MasterPageFile="~/Views/GatorShare.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h2>TorrentData Service</h2>
    <p>This service exposes BitTorrent related runtime information to the client.</p>
    <ul>
        <li>Download torrent file: GET /TorrentData/{<em>namespace</em>}/{<em>name</em>}/File</li>
    </ul>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="head" runat="server">
</asp:Content>
