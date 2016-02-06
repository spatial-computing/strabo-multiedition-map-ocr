using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.IO;
using Json;
using Strabo.Core.TextRecognition;
using Strabo.Core.Utility;
using com.wcohen.ss;
using System.Text.RegularExpressions;
using Nest;


namespace StringComparison
{

    class Program
    {
        static void Main(string[] args)
        {
            Strabo.Core.Utility.Log.SetLogDir(@"C:\Users\ronald\Documents\StringComparison\Input\");
            Log.WriteLine("begin");
            List<List<stringholder>> hack = new List<List<stringholder>>();

            String[] folderpath = Directory.GetFiles(@"C:\Users\ronald\Documents\StringComparison\Input\Data");
            foreach (String pathname in folderpath)
            {
                List<stringholder> list1 = new List<stringholder>();
                String tempread = System.IO.File.ReadAllText(pathname);
                var tempob = JsonParser.Deserialize(tempread);
                var tempobs = tempob.features;
                foreach(var item in tempobs){
                    String inputname = item["NameBeforeDictionary"];
                    var obj = item["geometry"]["coordinates"];
                    double xcord = 0;
                    double ycord = 0;
                    double xtemp = 0;
                    double ytemp = 0;
                    double xlength = 0;
                    double ylength = 0;
                    foreach (var inneritem in obj[0])
                    {
                        if (xtemp == 0 && ytemp == 0)
                        {
                            xtemp = inneritem[0];
                            ytemp = inneritem[1];
                        }
                        else
                        {
                            if (xtemp != inneritem[0])
                            {
                                xlength = Math.Abs(xtemp - inneritem[0]);
                                xcord = Math.Min(xtemp, inneritem[0]) + xlength / 2;
                            }
                            if (ytemp != inneritem[1])
                            {
                                ylength = Math.Abs(ytemp - inneritem[1]);
                                ycord = Math.Min(ytemp, inneritem[1]) + ylength / 2;
                            }
                        }                          
                       // xcord += inneritem[0];
                       // ycord += inneritem[1];
                    }
                    //xcord /= 4;
                   //ycord /= 4;
                    stringholder ts = new stringholder(inputname, xcord, ycord);
                    double ratio = xlength / ylength;
                    if (ratio > 2)
                        ts.doable = true;
                    else
                        ts.doable = false;
                    ts.length = xlength;
                    ts.height = ylength;
                    list1.Add(ts);
                }
                hack.Add(list1);
            }
            var node = new Uri("http://localhost:9200");

            var settings = new ConnectionSettings(
    node,
    defaultIndex: "frequency"
);

            ElasticClient client = new ElasticClient(settings);
            // group nearby strings into one
            for (int i = 0; i < hack.Count(); i++)
            {
                List<stringholder> listtemp = new List<stringholder>();
                List<stringholder> listnew = new List<stringholder>();
                Queue<stringholder> queue1 = new Queue<stringholder>();


                for (int j = 0; j < hack[i].Count(); j++)
                {
                    if (!hack[i][j].flag)
                    {
                    queue1.Enqueue(hack[i][j]);
                    hack[i][j].flag = true;
                    while (queue1.Count() > 0)
                    {
                        stringholder tempstring = queue1.Dequeue();
                        listtemp.Add(tempstring);
                        if (tempstring.doable == true)
                        {
                            for (int k = 0; k < hack[i].Count(); k++)
                            {
                                if (!hack[i][k].flag)
                                {
                                    //double dis = Math.Sqrt(Math.Pow((tempstring.xcord - hack[i][k].xcord), 2) + Math.Pow((tempstring.ycord - hack[i][k].ycord), 2));
                                    double xdis = Math.Abs(tempstring.xcord - hack[i][k].xcord) - tempstring.length / 2 - hack[i][k].length/2;
                                    //double ydis = Math.Abs(tempstring.ycord - hack[i][k].ycord) -tempstring.height / 2 - hack[i][k].height / 2;
                                    double ydis = Math.Abs(tempstring.ycord - hack[i][k].ycord);
                                    double xthrs = Math.Max(tempstring.height, hack[i][k].height)*4;
                                    //double ythrs = tempstring.height /5;
                                    double ythrs = tempstring.height*2;
                                    double heightratio = Math.Max(tempstring.height, hack[i][k].height) / Math.Min(tempstring.height, hack[i][k].height);
                                    if (ydis <= ythrs && xdis < xthrs && heightratio < 1.8)
                                    {
                                        if (hack[i][k].doable == true)
                                        {


                                            queue1.Enqueue(hack[i][k]);
                                            hack[i][k].flag = true;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    if (listtemp.Count == 1)
                    {

                            listnew.Add(listtemp[0]);
                    }
                    else
                    {

                        List<stringholder> te = new List<stringholder>();
                        for (int ii = 0; ii < listtemp.Count; ii++)
                        {
                            if (te.Count == 0)
                                te.Add(listtemp[ii]);
                            else
                            {
                                bool flag = false;
                                for (int jj = 0; jj < te.Count; jj++)
                                {

                                    if (Math.Abs(te[jj].ycord - listtemp[ii].ycord) < 10)
                                    {
                                        if (te[jj].xcord > listtemp[ii].xcord)
                                        {
                                            te.Insert(jj, listtemp[ii]);
                                            flag = true;
                                            break;
                                        }
                                    }
                                    else if (te[jj].ycord < listtemp[ii].ycord)
                                    {
                                        te.Insert(jj, listtemp[ii]);
                                        flag = true;
                                        break;
                                    }
                                }
                                if (!flag)
                                    te.Add(listtemp[ii]);
                            }
                        }
                        //RONALD
                        int left_bound = 0;
                        for (int ii = te.Count; ii > left_bound; ii--)
                        {
                            bool found = false;
                            int bitmask = Convert.ToInt32(Math.Pow(2, ii)-1);
                            //while (bitmask >= 0)
                            {
                                string check = "";
                                for(int jj=left_bound; jj<ii;jj++)
                                {
                                    check += te[jj].st;
                                    check += " ";
                                }
                                int fuzziness = 1;
                                if (check.Length > 3)
                                    fuzziness++;
                                if (check.Length > 4)
                                    fuzziness++;
                                if (check.Length > 5)
                                    fuzziness++;
                                if (check.Length > 7)
                                    fuzziness++;
                                if (check.Length > 9)
                                    fuzziness++;
                                double minsim = 1- (double) fuzziness/(double) check.Length-.1;
                                //double minsim = 0.7;
                                var searchResults = client.Search<Strabo.Core.TextRecognition.CheckDictionaryElasticSearchTemp.Frequency>(s => s
                                .From(0)
                                 .Size(10)
                                .Index("entity")
                                .Type("entity")
                                 .Query(q => q
                               .FuzzyLikeThis(fz => fz.OnFields(w => w.name).LikeText(check).MaxQueryTerms(20000).MinimumSimilarity(minsim))
                               )
                                );
                                if (searchResults.Documents.Count()>0)
                                {
                                    
                                    double xcord = 0;
                                    double ycord = 0;
                                    for (int jj = left_bound; jj < ii; jj++)
                                    {
                                        xcord += te[jj].xcord;
                                        ycord += te[jj].ycord;
                                    }
                                    xcord /= te.Count;
                                    ycord /= te.Count;
                                    listnew.Add(new stringholder(check, xcord, ycord));
                                    left_bound = ii;
                                    ii = te.Count+1;
                                }
                            }

                        }
                       
                    }
                    listtemp.Clear();
                }
                }
                hack[i].Clear();
                hack[i].AddRange(listnew);
                listnew.Clear();
            }
            
            // do the comparison
            List<String> result = new List<string>();
            StringComparison st = new StringComparison(hack);
            result = st.Doreplacement();

            for (int i = 0; i < result.Count; i++)
            {
                Console.WriteLine(result[i]);
            }
        }
    }

    public class StringStruct
    {
        public String selectedstring;
       // public Dictionary<String, double> listofword;
        public Dictionary<String, double> listofword;
       // public List<Dictionary<String, double>> listofsepword;
        public double xcord;
        public double ycord;

        public StringStruct(String s)
        {
            this.selectedstring = s;
            listofword = new Dictionary<String,double>();
            //listofsepword = new List<Dictionary<String, double>>();
        }
    }

    public class outputclass
    {
        public List<String> outputString = new List<string>();
        public Dictionary<String, Double> outputDic = new Dictionary<string,double>();
    }

    public class stringholder
    {
        public string st;
        public double xcord;
        public double ycord;
        public bool flag = false;
        public bool doable =false;
        public double length = 0;
        public double height = 0;

        public stringholder(string s, double x, double y)
        {
            this.st = s;
            this.xcord = x;
            this.ycord = y;
            flag = false;
        }
    }


    public class StringComparison
    {
        List<List<stringholder>> TessResult;

        public StringComparison(List<List<stringholder>> temp)
        {
            this.TessResult = temp;
        }


        public List<String> Doreplacement()
        {

           //string tempt = CheckDictionaryElasticSearchTemp.getDictionaryWord(tr.dict_word3,2 );

            List<outputclass> OutputResult = new List<outputclass>();

            double thredsholdvalue = 0.5; // lowest value for string comparison
            List<String> Dic = new List<string>();
            int mincharcount = 4;
           // String missingchar = null;
            JaccardDistance jc = new JaccardDistance(3);
            List<StringStruct> SelectedString = new List<StringStruct>();
            int z = TessResult.Count;
            Dictionary<String, double> totallist = new Dictionary<string, double>();
            Dictionary<String, double> comparelist = new Dictionary<string, double>();
            List<String> resultlist = new List<string>();

            List<StringStruct> queuelist = new List<StringStruct>();

            Dictionary<String, String> justforcheck = new Dictionary<string, String>();
            SmithWaterman sm = new SmithWaterman();
            com.wcohen.ss.NeedlemanWunsch nd = new com.wcohen.ss.NeedlemanWunsch();

            while (TessResult[0].Any())
            {

                if (TessResult[0][0].st.Count() > mincharcount)
                {

                outputclass temoutput = new outputclass();



                for (int s = 0; s < z; s++)
                {
                    SelectedString.Add(new StringStruct(null));

                }

                double value = 0;
            
                for (int i = 1; i < z; i++)
                {
                    value = 0;
                    int jmark = -1;
                    for (int j = 0; j < TessResult[i].Count(); j++)
                    {
                        if (!string.IsNullOrEmpty(TessResult[0][0].st) && !string.IsNullOrEmpty(TessResult[i][j].st))
                        {
                            //double tempS = Strabo.Core.TextRecognition.NeedlemanWunsch.findSimScore(TessResult[0][0].st, TessResult[i][j].st);
                            double tempS = nd.score(TessResult[0][0].st, TessResult[i][j].st);
                            double distance = Math.Sqrt(Math.Pow((TessResult[0][0].xcord - TessResult[i][j].xcord), 2) + Math.Pow((TessResult[0][0].ycord - TessResult[i][j].ycord), 2));
                            double smithstemp = sm.score(TessResult[0][0].st, TessResult[i][j].st);
                            tempS = Math.Max(tempS, smithstemp);
                            sm.explainScore(TessResult[0][0].st, TessResult[i][j].st);
                            if (distance < 300)
                            {
                                if (tempS > value && TessResult[i][j].st.Count() > mincharcount)
                                {

                                    value = tempS;
                                    if (value >= (thredsholdvalue * 2 * TessResult[0][0].st.Count()))
                                    {
                                        SelectedString[i].selectedstring = TessResult[i][j].st.Trim();        // put string with the largest value into the list
                                        jmark = j;
                                        SelectedString[i].xcord = TessResult[i][j].xcord;
                                        SelectedString[i].ycord = TessResult[i][j].ycord;
                                    }
                                }
                               /* else
                                {
                                    String[] splitword1 = TessResult[0][0].st.Split(' ');
                                    String[] splitword2 = TessResult[i][j].st.Split(' ');
                                    List<String> foo1 = new List<string>();
                                    List<String> foo2 = new List<string>();
                                    for (int ti = 0; ti < splitword1.Count(); ti++)
                                    {
                                        if (!splitword1[ti].Equals(""))
                                        {
                                            foo1.Add(splitword1[ti]);
                                        }
                                    }
                                    for (int ti = 0; ti < splitword2.Count(); ti++)
                                    {
                                        if (!splitword2[ti].Equals(""))
                                        {
                                            foo2.Add(splitword2[ti]);
                                        }
                                    }
                                    if ((foo1.Count > 1 || foo2.Count > 1) && foo1.Count < 4 && foo2.Count < 4 )
                                    {
                                        double maxmatch = 0;
                                        for (int index1 = 0; index1 < foo1.Count; index1++)
                                        {
                                            for (int index2 = 0; index2 < foo2.Count; index2++)
                                            {
                                                double insidetemp = Strabo.Core.TextRecognition.NeedlemanWunsch.findSimScore(foo1[index1], foo2[index2]);
                                                insidetemp *= (TessResult[0][0].st.Count() / foo1[index1].Count());
                                                if (insidetemp > maxmatch)
                                                    maxmatch = insidetemp;
                                            }
                                        }
                                        if (maxmatch > value && TessResult[i][j].st.Count() > 3)
                                        {

                                            value = maxmatch;
                                            if (value >= (thredsholdvalue * 3 * TessResult[0][0].st.Count()))
                                            {
                                                SelectedString[i].selectedstring = TessResult[i][j].st.Trim();        // put string with the largest value into the list
                                                jmark = j;
                                            }
                                        }
                                    }
                                }*/
                            }
                        }

                    }
                    if (jmark >= 0)
                        TessResult[i].RemoveAt(jmark);

                }
                SelectedString[0].selectedstring = TessResult[0][0].st;

                // get all missing selectedstring
                for (int e = 1; e < SelectedString.Count(); e++)
                {
                    if (!String.IsNullOrEmpty(SelectedString[e].selectedstring))
                    {
                        queuelist.Add(SelectedString[e]);
                    }
                }

                for (int e = 0; e < queuelist.Count(); e++)
                {
                    for (int ese = 1; ese < SelectedString.Count(); ese++)
                    {
                        double tempvalue = 0;
                        int jmark = -1;
                        if (String.IsNullOrEmpty(SelectedString[ese].selectedstring))
                        {
                            for (int j = 0; j < TessResult[ese].Count(); j++)
                            {
                                //double tempS = Strabo.Core.TextRecognition.NeedlemanWunsch.findSimScore(queuelist[e], TessResult[ese][j].st);
                                double tempS = nd.score(queuelist[e].selectedstring, TessResult[ese][j].st);
                                double smithstemp = sm.score(queuelist[e].selectedstring, TessResult[ese][j].st);
                                tempS = Math.Max(tempS, smithstemp);
                                double distance = Math.Sqrt(Math.Pow((queuelist[e].xcord - TessResult[ese][j].xcord), 2) + Math.Pow((queuelist[e].ycord - TessResult[ese][j].ycord), 2));
 
                                if (tempS > tempvalue && TessResult[ese][j].st.Count() > mincharcount)
                                {
                                    if (distance < 300)
                                    {
                                        tempvalue = tempS;
                                        if (tempS >= (thredsholdvalue * 2 * queuelist[e].selectedstring.Count()))
                                        {
                                            SelectedString[ese].selectedstring = TessResult[ese][j].st.Trim();
                                            jmark = j;
                                            SelectedString[ese].xcord = TessResult[ese][j].xcord;
                                            SelectedString[ese].ycord = TessResult[ese][j].ycord;
                                        }
                                    }
                                }
                               /* else
                                {
                                    String[] splitword1 = queuelist[e].Split(' ');
                                    String[] splitword2 = TessResult[ese][j].st.Split(' ');
                                    List<String> foo1 = new List<string>();
                                    List<String> foo2 = new List<string>();
                                    for (int ti = 0; ti < splitword1.Count(); ti++)
                                    {
                                        if (!splitword1[ti].Equals(""))
                                        {
                                            foo1.Add(splitword1[ti]);
                                        }
                                    }
                                    for (int ti = 0; ti < splitword2.Count(); ti++)
                                    {
                                        if (!splitword2[ti].Equals(""))
                                        {
                                            foo2.Add(splitword2[ti]);
                                        }
                                    }
                                    if ((foo1.Count > 1 || foo2.Count > 1) && foo1.Count < 4 && foo2.Count < 4)
                                    {
                                        double maxmatch = 0;
                                        for (int index1 = 0; index1 < foo1.Count; index1++)
                                        {
                                            for (int index2 = 0; index2 < foo2.Count; index2++)
                                            {
                                                double insidetemp = Strabo.Core.TextRecognition.NeedlemanWunsch.findSimScore(foo1[index1], foo2[index2]);
                                                insidetemp *= (queuelist[e].Count() / foo1[index1].Count());
                                                if (insidetemp > maxmatch)
                                                    maxmatch = insidetemp;
                                            }
                                        }
                                        if (maxmatch > tempvalue && TessResult[ese][j].st.Count() > 3)
                                        {

                                            tempvalue = maxmatch;
                                            if (tempvalue >= (thredsholdvalue * 3 * queuelist[e].Count()))
                                            {
                                                SelectedString[ese].selectedstring = TessResult[ese][j].st.Trim();        // put string with the largest value into the list
                                                jmark = j;
                                            }
                                        }
                                    }
                                }*/
                            }
                        }
                        if (jmark >= 0)
                        {
                            TessResult[ese].RemoveAt(jmark);
                        }
                    }
                }
                queuelist.Clear(); 
                    // check dictionary
               
                    //NEW FUNCTION

                    for (int e = 0; e < SelectedString.Count(); e++)
                    {
                        temoutput.outputString.Add(SelectedString[e].selectedstring);
                        if (!String.IsNullOrEmpty(SelectedString[e].selectedstring))
                        {

                            List<String> templ = null;
                           /* String select = SelectedString[e].selectedstring.Replace(" ", "");
                            bool capflag = false;
                            bool spiltflag = false;
                            for(int i = 1; i < select.Length; i++){
                                if (char.IsUpper(select[i]))
                                {
                                    capflag = !capflag;
                                }
                                else
                                {
                                    if (capflag)
                                    {
                                        spiltflag = true;
                                        break;
                                    }
                                }
                            }

                            if (spiltflag)
                            {
                                var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
                                string afterhandle = r.Replace(select, " ");
                                String[] splitword = afterhandle.Split(' ');
                                List<String> foo = new List<string>();
                                for (int ti = 0; ti < splitword.Count(); ti++)
                                {
                                    if (!splitword[ti].Equals("") || splitword[ti].Length > 2)
                                    {
                                        foo.Add(splitword[ti]);
                                    }
                                }

                                if (foo.Count() > 0)
                                {
                                    List<List<String>> temphold = new List<List<string>>();
                                    foreach (String word in foo)
                                    {
                                        List<String> splittempstring = Strabo.Core.TextRecognition.CheckDictionaryElasticSearchTemp.getDictionaryWord(word.Trim(), 2);
                                        if (splittempstring.Count > 50)
                                        {
                                            splittempstring.RemoveRange(50, splittempstring.Count - 51);
                                        }

                                        List<String> tempholdlist = new List<string>();
                                        for (int ii = 0; ii < splittempstring.Count(); ii++)
                                        {
                                            tempholdlist.Add(splittempstring[ii]);
                                        }
                                        if(tempholdlist.Count > 0)
                                            temphold.Add(tempholdlist);

                                    }
                                    List<String> tempstring = new List<string>();
                                    templ = temphold[0];
                                    for (int ii = 1; ii < temphold.Count; ii++)
                                    {
                                        for (int kk = 0; kk < temphold[ii].Count; kk++)
                                        {
                                            for (int jj = 0; jj < templ.Count; jj++)
                                            {
                                                tempstring.Add(templ[jj] + " " + temphold[ii][kk]);
                                            }
                                        }
                                        templ.Clear();
                                        templ.AddRange(tempstring);
                                        tempstring.Clear();
                                    }


                                }
                                templ.Count();
                            }
                            else
                            {*/
                                // SelectedString[e].listofword = CheckDictionaryElasticSearchTemp.getDictionaryWord(SelectedString[e].selectedstring, 2);
                                templ = Strabo.Core.TextRecognition.CheckDictionaryElasticSearchTemp.getDictionaryWord(SelectedString[e].selectedstring.Replace(" ", ""), 2);
                                //List<String> templ = new List<String>();
                                if (templ.Count > 100)
                                {
                                    templ.RemoveRange(100, templ.Count - 101);
                                }
                                /* if (templ.Count() < 1)
                                 {
                                     String[] splitword = SelectedString[e].selectedstring.Split(' ');
                                     List<String> foo = new List<string>();
                                     for (int ti = 0; ti < splitword.Count(); ti++)
                                     {
                                         if (!splitword[ti].Equals(""))
                                         {
                                             foo.Add(splitword[ti]);
                                         }
                                     }
                                     if (foo.Count() > 1)
                                     {
                                         List<List<String>> temphold = new List<List<string>>();
                                         foreach (String word in foo)
                                         {
                                             List<String> splittempstring = CheckDictionaryElasticSearchTemp.getDictionaryWord(word, 2);
                                             if (splittempstring.Count > 50)
                                             {
                                                 splittempstring.RemoveRange(50, splittempstring.Count - 51);
                                             }
                                            // Dictionary<String, Double> tempd = new Dictionary<string, double>();
                                             List<String> tempholdlist = new List<string>();
                                             for (int ii = 0; ii < splittempstring.Count(); ii++)
                                             {
                                                // float needlemanvalue = NeedlemanWunsch.findSimScore(word, splittempstring[ii]);
                                                 //tempd.Add(splittempstring[ii], (needlemanvalue / SelectedString[e].selectedstring.Count()));
                                                 tempholdlist.Add(splittempstring[ii]);
                                             }
                                             //SelectedString[e].listofsepword.Add(tempd);
                                             temphold.Add(tempholdlist);
                                       
                                         }
                                         List<String> tempstring = new List<string>();
                                         templ = temphold[0];
                                         for (int ii = 1; ii < temphold.Count; ii++)
                                         {
                                             for(int kk = 0; kk < temphold[ii].Count; kk ++){
                                                 for (int jj = 0; jj < templ.Count; jj++)
                                                 {
                                                     tempstring.Add(templ[jj]+ " "+temphold[ii][kk]);
                                                 }
                                             }
                                             templ.Clear();
                                             templ.AddRange(tempstring);
                                             tempstring.Clear();
                                         }

                                    
                                     }
                                     templ.Count();
                                 }*/
                            //}
                               for (int ii = 0; ii < templ.Count(); ii++)
                                {
                                    //double needlemanvalue = Strabo.Core.TextRecognition.NeedlemanWunsch.findSimScore(SelectedString[e].selectedstring, templ[ii]);
                                    double needlemanvalue = nd.score(SelectedString[e].selectedstring, templ[ii]) + 15;
                                    double smithvalue = sm.score(SelectedString[e].selectedstring, templ[ii]);
                                    double tempv = (Math.Max(needlemanvalue, needlemanvalue) / (2 * SelectedString[e].selectedstring.Count()));
                                    if (needlemanvalue > 9)
                                    {
                                        if (!SelectedString[e].listofword.ContainsKey(templ[ii]))
                                            SelectedString[e].listofword.Add(templ[ii], tempv);
                                    }

                                }
                      
                        }
                    }


                //check occurance
                for (int e = 0; e < SelectedString.Count(); e++)
                {
                    if (!String.IsNullOrEmpty(SelectedString[e].selectedstring))
                    {
                        if (SelectedString[e].listofword.Count() == 1)
                        {
                            if (totallist.ContainsKey(SelectedString[e].listofword.First().Key))
                                totallist[SelectedString[e].listofword.First().Key] += 1000;
                            else
                                totallist[SelectedString[e].listofword.First().Key] = 1000;
                        }
                        else if (SelectedString[e].listofword.Count() > 1)
                        {
                            foreach (var item in SelectedString[e].listofword)
                            {
                                if (totallist.ContainsKey(item.Key))
                                    totallist[item.Key]+= item.Value;
                                else
                                    totallist[item.Key] = item.Value;
                            }
                        }
                    }
                }

                var sortedDict = from entry in totallist orderby entry.Value descending select entry;
                sortedDict.ToDictionary(pair => pair.Key, pair => pair.Value);

                List<String> templist = new List<string>();                
                Dictionary<String, double> tempdic = new Dictionary<string,double>();
                foreach (var item in sortedDict)
                {
                    templist.Add(item.Key);
                    
                }

                if (templist.Count > 20)
                {
                    templist.RemoveRange(20, templist.Count - 21);
                }

                for(int ind = 0; ind < templist.Count; ind++){
                    tempdic.Add(templist[ind], 0);
                    for(int indx = 0; indx < SelectedString.Count; indx++){
                        if(!String.IsNullOrEmpty(SelectedString[indx].selectedstring)){
                             tempdic[templist[ind]] += (nd.score(templist[ind], SelectedString[indx].selectedstring)  / (2 * templist[ind].Count()));
                             //tempdic[templist[ind]] += (sm.score(templist[ind], SelectedString[indx].selectedstring)  / (2 * templist[ind].Count()));
                        }
                    }
                }

                var sortedDict2 = from entry in tempdic orderby entry.Value descending select entry;
                sortedDict2.ToDictionary(pair => pair.Key, pair => pair.Value);

                foreach (var item in sortedDict2)
                {
                    temoutput.outputDic.Add(item.Key, item.Value);
                }
               
                OutputResult.Add(temoutput);
            } 
               
                TessResult[0].RemoveAt(0);

                //clear everything
                SelectedString.Clear();
                totallist.Clear();


            }

            Microsoft.Office.Interop.Excel.Application oXL;
            Microsoft.Office.Interop.Excel._Workbook oWB;
            Microsoft.Office.Interop.Excel._Worksheet oSheet;
            Microsoft.Office.Interop.Excel.Range oRng;
            object misvalue = System.Reflection.Missing.Value;

            //Start Excel and get Application object.
            oXL = new Microsoft.Office.Interop.Excel.Application();
            oXL.Visible = true;

            //Get a new workbook.
            oWB = (Microsoft.Office.Interop.Excel._Workbook)(oXL.Workbooks.Add(""));
            oSheet = (Microsoft.Office.Interop.Excel._Worksheet)oWB.Worksheets[1];


            for (int i = 0; i < OutputResult.Count; i++)
            {
                int j = i + 1;
                int k = 0;
                for (k = 0; k < OutputResult[i].outputString.Count; k++)
                {
                    int kk = k + 1;
                    if (!String.IsNullOrEmpty(OutputResult[i].outputString[k]))
                        oSheet.Cells[j, kk] = OutputResult[i].outputString[k];
                }
                k = k + 1;
                if (OutputResult[i].outputDic.Count > 0)
                    oSheet.Cells[j, k] = OutputResult[i].outputDic.First().Key;

            }

            oWB.SaveAs(@"C:\Users\ronald\Documents\StringComparison\Output\result11.xls", Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing,
        false, false, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange,
        Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

            oWB.Close();



            OutputResult.Count();
            Log.WriteLine("END");
            return resultlist;

        }


    }

}
