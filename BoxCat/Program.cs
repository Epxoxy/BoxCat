using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxCat {
    class Program {
        static void Main(string[] args) {
            
            var I = new Person();
            var note = new Note();

            Console.WriteLine("Working....");
            for (int n = 0; n < 1000000; n++) {

                //拿东西,需要箱子,放射物,猫
                //declare variables, we need box, radioactive substance and cat.
                var box = new Box();
                var substance = new RadioactiveSubstance();
                var cat = new Cat();

                //把猫和放射物放进箱子关好
                //put the cat and the radioactive substance into the box and close it.
                I.put(cat.and(substance)).into(box)
                    .then(theBox => I.close(theBox));

                //打开箱子看猫并记录结果
                //open the box then find the cat to observe.
                //recording the result of this time.
                I.open(box).find<Cat>().observe(theCat => theCat?.isAlive().LastOrDefault())
                    .then(theRecord => I.write(theRecord)).to(note);
            }
            var alive = note.Records.Count(r => r.Value is bool && (bool)r.Value);
            var died = note.Records.Count - alive;
            Console.WriteLine($"Alive {alive}, Died {died}");
            Console.ReadKey();
        }
    }

    public class Person {
        
        public Box.Putable put(IEnumerable<object> objs) {
            return new Box.Putable(objs);
        }

        public EyeView open(Box box) {
            return mapView(box?.toOpen());
        }

        public void close(Box box) {
            box?.toClose();
        }

        public Note.Writable write(Note.Record record) {
            return new Note.Writable(record);
        }

        protected EyeView mapView(List<object> sth) {
            return new EyeView(sth);
        }

        public class EyeView {
            public static readonly EyeView noNew = new EyeView(new List<object>());
            private List<object> data;

            public EyeView(List<object> data) {
                this.data = data ?? new List<object>();
            }

            public Observable<T> find<T>() {
                foreach (object obj in data) {
                    if (obj is T) {
                        return new Observable<T>((T)obj);
                    }
                }
                return new Observable<T>(default(T));
            }

            public class Observable<T> {
                private T observed;

                public Observable(T observed) {
                    this.observed = observed;
                }

                public Continuable<Note.Record> observe(Func<T, object> onObserver) {
                    return new Continuable<Note.Record>(new Note.Record(DateTime.Now, onObserver.Invoke(observed)));
                }
            }
        }
    }
    
    public class Box {
        private List<object> Content { get; set; } = new List<object>();
        public IReadOnlyList<object> ReadonlyContent => Content;
        public bool IsOpen { get; private set; }

        public void put(object obj) {
            Content.Add(obj);
        }

        public List<object> toOpen() {
            IsOpen = true;
            foreach (object obj in Content) {
                IAdaptable adaptable = obj as IAdaptable;
                if (adaptable != null) {
                    adaptable.adapt(Content, IsOpen);
                }
            }
            return Content;
        }

        public void toClose() {
            IsOpen = false;
            var products = new List<object>();
            do {
                products.Clear();
                foreach (var obj in Content) {
                    var adaptable = obj as IAdaptable;
                    if (adaptable != null) {
                        var newProducts = adaptable.adapt(Content, IsOpen);
                        foreach (var thing in newProducts) {
                            if (!Content.Contains(thing)) {
                                products.Add(thing);
                            }
                        }
                    }
                }
                Content.AddRange(products);
            } while (products.Count > 0);
        }

        public class Putable {
            private List<object> ownList = new List<object>();

            public Putable() { }

            public Putable(IEnumerable<object> objs) {
                ownList.AddRange(objs);
            }

            public Putable and(object obj) {
                ownList.Add(obj);
                return this;
            }

            public Continuable<Box> into(Box box) {
                foreach (object owned in ownList) {
                    box.put(owned);
                }
                return new Continuable<Box>(box);
            }
        }
        
    }

    public class Cat : IAdaptable {
        public object Alive { get; private set; } = new CertainState<bool>(true);

        public List<object> adapt(List<object> env, bool observed) {
            if (!observed) {
                List<object> product = new List<object>();
                MixedState<bool> aliveTemp = MixedState<bool>.convert(Alive);
                Alive = aliveTemp;
                foreach (object thing in env) {
                    if (thing is PoisonGas) {
                        product.Add("Oops!");
                        product.Add("@^)$(@*&*#(!");
                        aliveTemp.mix(false);
                        return product;
                    }
                }
            } else {
                Alive = MixedState<bool>.toCertain(Alive);
            }
            return new List<object>();
        }
        
        public IEnumerable<bool> isAlive() {
            if (Alive is CertainState<bool>) {
                return new bool[] { ((CertainState<bool>)Alive).State };
            }
            return ((MixedState<bool>)Alive).States;
        }
        
    }
    
    public class RadioactiveSubstance : IAdaptable {
        public object IsRadioactive { get; private set; } = new CertainState<bool>(true);
        private static readonly Random random = new Random();
        private ProcessVariable pv;

        private bool checkRadioactive() {
            return IsRadioactive is CertainState<bool> 
                && ((CertainState<bool>)IsRadioactive).State;
        }
        
        public List<object> adapt(List<object> env, bool observed) {
            if (!observed) {
                if (checkRadioactive() && random.Next(0, 99999) > 50000) {
                    MixedState<bool> radioactiveTemp = MixedState<bool>.convert(IsRadioactive);
                    radioactiveTemp.mix(false);
                    IsRadioactive = radioactiveTemp;
                    return decay().Variables;
                }
            } else {
                IsRadioactive = MixedState<bool>.toCertain(IsRadioactive);
            }
            return new List<object>();
        }

        public ProcessVariable decay() {
            if(pv == null) {
                pv = new ProcessVariable();
                pv.Variables.Add(new PoisonGas());
            }
            return pv;
        }

        public class ProcessVariable {
            public List<object> Variables { get; private set; } = new List<object>();

        }
    }
    
    public class PoisonGas {

    }

    public class Note {
        private List<Record> records = new List<Record>();
        public IReadOnlyList<Record> Records => records;

        public void record(Record record) {
            records.Add(record);
        }

        public class Writable {
            private Record record;
            public Writable(Record record) {
                this.record = record;
            }
            public void to(Note note) {
                note.record(record);
            }
        }

        public class Record {
            public DateTime Time { get; private set; }
            public object Value { get; private set; }
            public String Header { get; set; }
            public Record(DateTime time, object value) {
                this.Time = time;
                this.Value = value;
            }

            public override string ToString() {
                return $"Time {Time}, Header {Header}, Value {Value}";
            }
        }

    }

    public interface IAdaptable {
        List<object> adapt(List<object> env, bool observed);
    }

    public class CertainState<T> {
        public T State { get; private set; }

        public CertainState(T state) {
            this.State = state;
        }
    }

    public class MixedState<T> {
        private List<T> states = new List<T>();
        public IReadOnlyList<T> States => states;

        public MixedState(T state) {
            this.states.Add(state);
        }

        public bool mix(T state) {
            if (this.states.Contains(state)) {
                return false;
            }
            this.states.Add(state);
            return true;
        }

        public void outFrom(T state) {
            this.states.Remove(state);
        }

        public T last() {
            if (states.Count > 0) {
                return states[states.Count - 1];
            }
            return default(T);
        }

        public CertainState<T> toCertain() {
            return new CertainState<T>(last());
        }

        public static MixedState<T> convert(object obj) {
            if (obj is MixedState<T>) {
                return (MixedState<T>)obj;
            } else if (obj is CertainState<T>) {
                return new MixedState<T>(((CertainState<T>)obj).State);
            } else {
                throw new NotImplementedException();
            }
        }

        public static object toCertain(object obj) {
            if (obj is MixedState<T>) {
                return ((MixedState<T>)obj).toCertain();
            }
            return obj;
        }
    }

    public class Continuable<T> {
        private T ctx;

        public Continuable(T ctx) {
            this.ctx = ctx;
        }

        public void then(Action<T> action) {
            action?.Invoke(ctx);
        }

        public R then<R>(Func<T, R> func) {
            if (func == null)
                return default(R);
            return func.Invoke(ctx);
        }
    }

    public static class Utils {
        public static List<object> and(this object obj, params object[] objs) {
            List<object> ownList = new List<object>(objs);
            ownList.Add(obj);
            return ownList;
        }
    }

}
