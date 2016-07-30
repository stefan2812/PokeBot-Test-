using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.GeneratedCode;
using PokemonGo.RocketAPI.Helpers;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PokemonGo.RocketAPI.Console
{
    public partial class Pokemons : Form
    {
        private static Client client;
        private static GetPlayerResponse profile;
        private static GetInventoryResponse inventory;
        private static IOrderedEnumerable<PokemonData> pokemons;
        public class taskResponse
        {
            public bool Status { get; set; }
            public string Message { get; set; }
            public taskResponse() { }
            public taskResponse(bool status, string message)
            {
                Status = status;
                Message = message;
            }
        }
        public Pokemons()
        {
            InitializeComponent();
            ClientSettings = new Settings();
        }

        public static ISettings ClientSettings;

        private void Pokemons_Load(object sender, EventArgs e)
        {
            textBox2.Text = "60";
            Execute();
        }

        private async void Execute()
        {
            EnabledButton(false);
            textBox1.Text = "Aktualisiere Pokemon Übersicht.";

            client = new Client(ClientSettings);

            try
            {
                switch (ClientSettings.AuthType)
                {
                    case AuthType.Ptc:
                        await client.DoPtcLogin(ClientSettings.PtcUsername, ClientSettings.PtcPassword);
                        break;
                    case AuthType.Google:
                        await client.DoGoogleLogin();
                        break;
                }

                await client.SetServer();
                profile = await client.GetProfile();
                inventory = await client.GetInventory();
                pokemons =
                    inventory.InventoryDelta.InventoryItems
                    .Select(i => i.InventoryItemData?.Pokemon)
                        .Where(p => p != null && p?.PokemonId > 0)
                        .OrderByDescending(key => key.Cp);
                var families = inventory.InventoryDelta.InventoryItems
                    .Select(i => i.InventoryItemData?.PokemonFamily)
                    .Where(p => p != null && (int)p?.FamilyId > 0)
                    .OrderByDescending(p => (int)p.FamilyId);

                var imageSize = 50;

                var imageList = new ImageList { ImageSize = new Size(imageSize, imageSize) };
                listView1.ShowItemToolTips = true;

                var templates = await client.GetItemTemplates();
                var myPokemonSettings =  templates.ItemTemplates.Select(i => i.PokemonSettings).Where(p => p != null && p?.FamilyId != PokemonFamilyId.FamilyUnset);
                var pokemonSettings = myPokemonSettings.ToList();

                var myPokemonFamilies = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PokemonFamily).Where(p => p != null && p?.FamilyId != PokemonFamilyId.FamilyUnset);
                var pokemonFamilies = myPokemonFamilies.ToArray();

                listView1.DoubleBuffered(true);
                foreach (var pokemon in pokemons)
                {
                    Bitmap pokemonImage = null;
                    await Task.Run(() =>
                    {
                        pokemonImage = GetPokemonImage((int)pokemon.PokemonId);
                    });
                    imageList.Images.Add(pokemon.PokemonId.ToString(), pokemonImage);

                    listView1.LargeImageList = imageList;
                    var listViewItem = new ListViewItem();
                    listViewItem.Tag = pokemon;


                    var currentCandy = families
                        .Where(i => (int)i.FamilyId <= (int)pokemon.PokemonId)
                        .Select(f => f.Candy)
                        .First();
                    var currIv = Math.Round(Perfect(pokemon));
                    listViewItem.ImageKey = pokemon.PokemonId.ToString();

                    var pokemonId2 = pokemon.PokemonId;
                    var pokemonName = pokemon.Id;

                    listViewItem.Text = string.Format("{0}\n{1} CP", pokemon.PokemonId, pokemon.Cp);
                    listViewItem.ToolTipText = currentCandy + " Candy\n" + currIv + "% IV";

                    var settings = pokemonSettings.Single(x => x.PokemonId == pokemon.PokemonId);
                    var familyCandy = pokemonFamilies.Single(x => settings.FamilyId == x.FamilyId);

                    if (settings.EvolutionIds.Count > 0 && familyCandy.Candy > settings.CandyToEvolve)
                        listViewItem.Checked = true;

                    listView1.Items.Add(listViewItem);
                }
                Text = "Pokemon Übersicht | Nutzer: " + profile.Profile.Username + " | Pokemon: " + pokemons.Count() + "/" + profile.Profile.PokeStorage;
                EnabledButton(true);

                textBox1.Text = string.Empty;
            }
            catch (TaskCanceledException e) {
                textBox1.Text = e.Message;
                Execute();
            }
            catch (UriFormatException e) {
                textBox1.Text = e.Message;
                Execute();
            }
            catch (ArgumentOutOfRangeException e) {
                textBox1.Text = e.Message;
                Execute();
            }
            catch (ArgumentNullException e) {
                textBox1.Text = e.Message;
                Execute();
            }
            catch (NullReferenceException e) {
                textBox1.Text = e.Message;
                Execute();
            }
            catch (Exception e) {
                textBox1.Text = e.Message;
                Execute();
            }
        }

        private void EnabledButton(bool enabled)
        {
            button1.Enabled = enabled;
            button2.Enabled = enabled;
            button3.Enabled = enabled;
            btnUpgrade.Enabled = enabled;
            checkBox1.Enabled = enabled;
            textBox2.Enabled = enabled;
        }

        private static Bitmap GetPokemonImage(int pokemonId)
        {
            var Sprites = AppDomain.CurrentDomain.BaseDirectory + "Sprites\\";
            string location = Sprites + pokemonId + ".png";
            if (!Directory.Exists(Sprites))
                Directory.CreateDirectory(Sprites);
            if (!File.Exists(location))
            {
                WebClient wc = new WebClient();
                wc.DownloadFile("http://pokeapi.co/media/sprites/pokemon/" + pokemonId + ".png", @location);
            }
            PictureBox picbox = new PictureBox();
            picbox.Image = Image.FromFile(location);
            Bitmap bitmapRemote = (Bitmap)picbox.Image;
            return bitmapRemote;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Clear();
            Execute();
        }

        public static float Perfect(PokemonData poke)
        {
            return ((float)(poke.IndividualAttack + poke.IndividualDefense + poke.IndividualStamina) / (3.0f * 15.0f)) * 100.0f;
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (listView1.FocusedItem.Bounds.Contains(e.Location) == true)
                {
                    if (listView1.SelectedItems.Count > 1)
                    {
                        MessageBox.Show("Du kannst nur 1 Objekt für das Quick-Menu nutzen!", "Auswahl zu groß", MessageBoxButtons.OK);
                        return;
                    }
                    contextMenuStrip1.Show(Cursor.Position);
                }
            }
        }

        private async void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var pokemon = (PokemonData)listView1.SelectedItems[0].Tag;
            taskResponse resp = new taskResponse(false, string.Empty);

            if (MessageBox.Show(this, pokemon.PokemonId + " mit " + pokemon.Cp + " CP, das bedeutet " + Math.Round(Perfect(pokemon)) + "% perfekt", "Sicher das du es transferieren willst?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                resp = await transferPokemon(pokemon);
            }
            else
            {
                return;
            }
            if (resp.Status)
            {
                listView1.Items.Remove(listView1.SelectedItems[0]);
                Text = "Pokemon Übersicht | Nutzer: " + profile.Profile.Username + " | Pokemon: " + listView1.Items.Count + "/" + profile.Profile.PokeStorage;
            }
            else
                MessageBox.Show(resp.Message +" transfer fehlgeschlagen!", "Transfer Status", MessageBoxButtons.OK);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            EnabledButton(false);
            var selectedItems = listView1.SelectedItems;
            int evolved = 0;
            int total = selectedItems.Count;
            string failed = string.Empty;
            taskResponse resp = new taskResponse(false, string.Empty);

            foreach (ListViewItem selectedItem in selectedItems)
            {
                resp = await evolvePokemon((PokemonData)selectedItem.Tag);
                if (resp.Status)
                    evolved++;
                else
                    failed += resp.Message + " ";
            }

            if (failed != string.Empty)
                MessageBox.Show("Erfolgreich entwickelt: " + evolved + "/" + total + " Pokemon. Fehlgeschlagen: " + failed, "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Erfolgreich entwickelt: " + evolved + "/" + total + " Pokemon.", "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            listView1.Clear();
            Execute();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            EnabledButton(false);
            var selectedItems = listView1.SelectedItems;
            int transfered = 0;
            int total = selectedItems.Count;
            string failed = string.Empty;
            taskResponse resp = new taskResponse(false, string.Empty);

            foreach (ListViewItem selectedItem in selectedItems)
            {
                resp = await transferPokemon((PokemonData)selectedItem.Tag);
                if (resp.Status)
                {
                    listView1.Items.Remove(selectedItem);
                    transfered++;
                }
                else
                    failed += resp.Message + " ";

            }

            if(failed != string.Empty)
                MessageBox.Show("Erfolgreich Transferiert: " + transfered + "/" + total + " Pokemon. Fehlgeschlagen: " + failed, "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Erfolgreich Transferiert: " + transfered + "/" + total + " Pokemon.", "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Text = "Pokemon Übersicht | Nutzer: " + profile.Profile.Username + " | Pokemon: " + listView1.Items.Count + "/" + profile.Profile.PokeStorage;
            EnabledButton(true);
            //listView1.Clear();
            //Execute();
        }

        private async void btnUpgrade_Click(object sender, EventArgs e)
        {
            EnabledButton(false);
            var selectedItems = listView1.SelectedItems;
            int powerdup = 0;
            int total = selectedItems.Count;
            string failed = string.Empty;
            taskResponse resp = new taskResponse(false, string.Empty);

            foreach (ListViewItem selectedItem in selectedItems)
            {
                resp = await PowerUp((PokemonData)selectedItem.Tag);
                if (resp.Status)
                    powerdup++;
                else
                    failed += resp.Message + " ";
            }
            if (failed != string.Empty)
                MessageBox.Show("Erfolgreich geboostet: " + powerdup + "/" + total + " Pokemon. Fehlgeschlagen: " + failed, "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Erfolgreich geboostet: " + powerdup + "/" + total + " Pokemon.", "Transfer status", MessageBoxButtons.OK, MessageBoxIcon.Information);
            listView1.Clear();
            Execute();
        }

        private static async Task<taskResponse> evolvePokemon(PokemonData pokemon)
        {
            taskResponse resp = new taskResponse(false, string.Empty);
            try
            {
                var evolvePokemonResponse = await client.EvolvePokemon((ulong)pokemon.Id);

                if(evolvePokemonResponse.Result == EvolvePokemonOut.Types.EvolvePokemonStatus.PokemonEvolvedSuccess)
                {
                    resp.Status = true;
                }
                else
                {
                    resp.Message = pokemon.PokemonId.ToString();
                }

                await RandomHelper.RandomDelay(1000, 2000);
            }
            catch (TaskCanceledException) { await evolvePokemon(pokemon); }
            catch (UriFormatException) { await evolvePokemon(pokemon); }
            catch (ArgumentOutOfRangeException) { await evolvePokemon(pokemon); }
            catch (ArgumentNullException) { await evolvePokemon(pokemon); }
            catch (NullReferenceException) { await evolvePokemon(pokemon); }
            catch (Exception) { await evolvePokemon(pokemon); }
            return resp;
        }

        private static async Task<taskResponse> transferPokemon(PokemonData pokemon)
        {
            taskResponse resp = new taskResponse(false, string.Empty);
            try
            {
                var transferPokemonResponse = await client.TransferPokemon(pokemon.Id);

                if (transferPokemonResponse.Status == 1)
                {
                    resp.Status = true;
                }
                else
                {
                    resp.Message = pokemon.PokemonId.ToString();
                }
            }
            catch (TaskCanceledException) { await transferPokemon(pokemon); }
            catch (UriFormatException) { await transferPokemon(pokemon); }
            catch (ArgumentOutOfRangeException) { await transferPokemon(pokemon); }
            catch (ArgumentNullException) { await transferPokemon(pokemon); }
            catch (NullReferenceException) { await transferPokemon(pokemon); }
            catch (Exception) { await transferPokemon(pokemon); }
            return resp;
        }

        private static async Task<taskResponse> PowerUp(PokemonData pokemon)
        {
            taskResponse resp = new taskResponse(false, string.Empty);
            try
            {
                var evolvePokemonResponse = await client.PowerUp(pokemon.Id);

                if (evolvePokemonResponse.Result == EvolvePokemonOut.Types.EvolvePokemonStatus.PokemonEvolvedSuccess)
                {
                    resp.Status = true;
                }
                else
                {
                    resp.Message = pokemon.PokemonId.ToString();
                }

                await RandomHelper.RandomDelay(1000, 2000);
            }
            catch (TaskCanceledException) { await PowerUp(pokemon); }
            catch (UriFormatException) { await PowerUp(pokemon); }
            catch (ArgumentOutOfRangeException) { await PowerUp(pokemon); }
            catch (ArgumentNullException) { await PowerUp(pokemon); }
            catch (NullReferenceException) { await PowerUp(pokemon); }
            catch (Exception ex) { await PowerUp(pokemon); }
            return resp;
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (listView1.SelectedItems[0].Checked)
                contextMenuStrip1.Items[2].Visible = true;
        }

        private void contextMenuStrip1_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            contextMenuStrip1.Items[2].Visible = false;
        }

        private async void evolveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pokemon = (PokemonData)listView1.SelectedItems[0].Tag;
            taskResponse resp = new taskResponse(false, string.Empty);

            if (MessageBox.Show(this, pokemon.PokemonId + " mit " + pokemon.Cp + " CP, das bedeutet " + Math.Round(Perfect(pokemon)) + "% perfekt", "Willst du es wirklich entwickeln?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                resp = await evolvePokemon(pokemon);
            }
            else
            {
                return;
            }
            if (resp.Status)
            {
                listView1.Clear();
                Execute();
            }
            else
                MessageBox.Show(resp.Message + " Entwickeln fehlgeschlagen!", "Entwicklungsstatus", MessageBoxButtons.OK);
        }

        private async void powerUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var pokemon = (PokemonData)listView1.SelectedItems[0].Tag;
            taskResponse resp = new taskResponse(false, string.Empty);

            if (MessageBox.Show(this, pokemon.PokemonId + " mit " + pokemon.Cp + " CP, das bedeutet " + Math.Round(Perfect(pokemon)) + "% perfekt", "Möchtest du es wirklich Boosten?", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                resp = await PowerUp(pokemon);
            }
            else
            {
                return;
            }
            if (resp.Status)
            {
                listView1.Clear();
                Execute();
            }
            else
                MessageBox.Show(resp.Message + " Boosting fehlgeschlagen!", "Boosting Status", MessageBoxButtons.OK);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                int def = 0;
                int interval;
                if(int.TryParse(textBox2.Text, out interval))
                {
                    def = interval;
                }
                if (def < 30 || def > 3600)
                {
                    MessageBox.Show("Interval muss zwischen 30 und 3600 Sekunden betragen!");
                    textBox2.Text = "60";
                    checkBox1.Checked = false;
                }
                else
                {
                    timer1.Interval = def * 1000;
                    timer1.Start();
                }
            }
            else
            {
                timer1.Stop();
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Execute();
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
    public static class ControlExtensions
    {
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }
    }
}
