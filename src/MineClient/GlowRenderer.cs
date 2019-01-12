using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace MineClient
{
    public partial class GlowRenderer : Control
    {
        public Action<GlowRenderer, Graphics> Glow = new Action<GlowRenderer, Graphics>((t,g) =>
        {

            //using (var g = t.CreateGraphics())
            t.DrawLogic(g);
            //g.FillRectangle(Brushes.Red, background.GlowIteration * 5, 0, 10, background.Height);
            ++t.GlowIteration; if ((t.GlowIteration * t.Speed) - t.Width > t.Width) t.GlowIteration = -1;
        });


        public Image GlowTexture;
        public int Speed = 10;
        public int Tick = 20;
        public int GlowDelay = 3000;
        public int GlowIteration = -1;
        public bool Run = true;
        public System.Windows.Forms.Timer AnimationTimer = new System.Windows.Forms.Timer();
        public System.Windows.Forms.Timer InternalAnimation = new System.Windows.Forms.Timer();

        private void DrawLogic(Graphics g)
        {
            g.DrawImage(GlowTexture, (GlowIteration * Speed) - GlowTexture.Width, 0, GlowTexture.Width, Height);
        }

        public void Configure()
        {
            Form owner = FindForm();
            this.Location = new Point(0, 0);
            this.Size = new Size(owner.Width, owner.Height);
        }

        public GlowRenderer() : this(MineClient.Properties.Resources.glow, 1000)
        {

        }

        public GlowRenderer(Image GlowTexture, int GlowDelay)
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            Init(GlowTexture, GlowDelay);
        }

        private void Init(Image GlowTexture, int GlowDelay)
        {
            this.GlowTexture = GlowTexture;
            //SetStyle(ControlStyles.Opaque, true);
            this.BackColor = Color.Transparent;
            //Form owner = form;
            //Configure();
            this.GlowDelay = GlowDelay;
            AnimationTimer.Interval = GlowDelay;
            AnimationTimer.Tick += Animate;
            AnimationTimer.Start();
            InternalAnimation.Tick += InternalAnimation_Tick;
        }

        private int _backup;
        private int _frame;
        private void InternalAnimation_Tick(object sender, EventArgs e)
        {
            if (_frame > _backup)
            {
                InternalAnimation.Stop();
                this.GlowIteration = -1;
                _frame = 0;
                AnimationTimer.Interval = GlowDelay;
                return;
            }
            _frame += Speed;
            this.Invalidate();
        }

        //protected GlowRenderer() { }
        public void Animate()
        {
            Task.Factory.StartNew(() => { this.Invoke((Action) delegate { Animate(null, null); }); });
            Console.WriteLine("manual!");
        }
        void Animate(object sender, EventArgs e)
        {
             this.AnimationTimer.Interval = int.MaxValue;
            _backup = this.Width + GlowTexture.Width;
            
            InternalAnimation.Interval = Tick;
            _frame = this.GlowIteration = 0;
            InternalAnimation.Start();
            /*Task.Factory.StartNew(() =>
            {
                //animationTimer.Stop();
                int backUp = this.Width + GlowTexture.Width;
                this.GlowIteration = 0;
                for (int i = 0; i < backUp; i += Speed)
                {
                    this.Invoke((Action)delegate
                    {
                        this.Invalidate();
                    });
                    Thread.Sleep(15);
                }
                this.GlowIteration = -1;
                this.Invoke((Action)delegate
                {
                    this.Invalidate();
                });

            }).ContinueWith((t) => { this.Invoke((Action)delegate { AnimationTimer.Interval = GlowDelay; }); });*/

        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle = cp.ExStyle | 0x20;
                return cp;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
           base.OnPaint(e);
           if(Glow != null && GlowIteration != -1)
                Glow(this, e.Graphics);
        }
        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    Graphics g = e.Graphics;
        //    Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

        //    g.Dispose();
        //    base.OnPaint(e);
        //}

        //protected override void OnBackColorChanged(EventArgs e)
        //{
        //    if (this.Parent != null)
        //    {
        //        Parent.Invalidate(this.Bounds, true);
        //    }
        //    base.OnBackColorChanged(e);
        //}

        //protected override void OnParentBackColorChanged(EventArgs e)
        //{
        //    this.Invalidate();
        //    base.OnParentBackColorChanged(e);
        //}
    }
}
