using System;
using System.Collections.Generic;

namespace NikkeRese
{
    class program
    {
        static void Main(string[] args)
        {
            RandomBox rb = new RandomBox();
            Dictionary<string, int> History = new Dictionary<string, int>();
            int gatchaPoint = 60;   //가챠 가능한 횟수
            int GCCount = 0;    //가챠 한 횟수 (한 계정씩 초기화 됨)
            int RSCount = 0;    //시도한 계정의 갯수
            bool isEnded = false;   //홍련을 뽑았다면 True로 하고 리세를 멈춘다

            rb.Insert("홍련", 0.0833f);
            rb.Insert("화란", 0.0833f);
            rb.Insert("라푼젤", 0.0833f);
            rb.Insert("노아", 0.0833f);
            rb.Insert("이사벨", 0.0833f);
            rb.Insert("스노우화이트", 0.0833f);
            rb.Insert("리타", 0.0921f);
            rb.Insert("1티어", 0.4605f);
            rb.Insert("2티어", 0.6447f);
            rb.Insert("잡SRR", 2.3029f);
            rb.Insert("SSR 이하", 96);

            while (true)
            {
                if (GCCount != 0)
                {
                    var picked = rb.Pick();
                    Console.WriteLine(picked);

                    if (History.ContainsKey(picked))
                        History[picked]++;
                    else
                        History.Add(picked, 1);

                    if (picked == "홍련")
                        isEnded = true;

                    GCCount--;
                }
                else
                {
                    if (isEnded)
                        break;
                    GCCount = gatchaPoint;
                    History = new Dictionary<string, int>();
                    RSCount++;
                }
            }
            Console.WriteLine();
            Console.WriteLine("리세마라 종료");
            Console.WriteLine("-----------------------------------------------");
            foreach (var item in History)
            {
                Console.WriteLine(@"{0} : {1}번 등장", item.Key, item.Value);
            }
            Console.WriteLine(@"{0}번째 계정", RSCount);
            Console.ReadKey();
        }
    }
    class RandomBox
    {
        ///<summary>이름 검색을 위한 Trie자료구조의 시작위치입니다.</summary>
        private TrieNode rootTrie = new TrieNode();

        ///<summary>변동확률을 가진 아이템 중 첫 번째 아이템입니다.</summary>
        private Content rootFlexible = null;
        ///<summary>부동확률을 가진 아이템 중 첫 번째 아이템입니다.</summary>
        private Content rootNonFlexible = null;

        ///<summary>모든 변동확률의 합을 표시합니다.</summary>
        private float rateFlexible = 0f;
        ///<summary>모든 부동확률의 합을 표시합니다.</summary>
        private float rateNonFlexible = 0f;

        ~RandomBox()
        {
            if (rootTrie != null)
            {
                rootTrie.Destroy();
                rootTrie = null;
            };

            if (rootFlexible != null)
            {
                rootFlexible.Destroy();
                rootFlexible = null;
            };

            if (rootNonFlexible != null)
            {
                rootNonFlexible.Destroy();
                rootNonFlexible = null;
            };
        }

        ///<summary>랜덤박스의 아이템을 뽑아 문자열로 반환합니다. 뽑을 것이 없으면 null을 반환합니다.</summary>
        public string Pick()
        {
            //남은 확률을 표시합니다. 0~1사이에서 시작하여, 아이템을 체크할 때마다 줄어듭니다. 0이 되면 해당 아이템을 반환하게 합니다.
            float rateLeft = Convert.ToSingle(new Random().NextDouble());
            //부동확률을 먼저 계산하여 해당하는 아이템이 있는지 확인합니다.
            Content result = null;
            if (rootNonFlexible != null)
            {
                result = rootNonFlexible.RateCheck(rateLeft, 1.0f, out rateLeft);
            };
            //해당하는 아이템이 나온 경우
            if (result != null)
            {
                //대상이 더 이상 뽑을 수 없는 상태가 된 경우 부동확률 총합을 줄입니다.
                if (!result.AvailableCheck())
                {
                    rateNonFlexible -= result.GetRate();
                };
                //대상의 이름을 반환합니다.
                return result.GetName();
            }
            //해당하는 아이템이 나오지 않은 경우
            else
            {
                //변동확률에서 아이템을 찾습니다.
                //전체 변동확률을 남은 확률에 사상하여 확률을 계산합니다.
                //예)부동확률을 빼고 남은 확률이 0.5이고, 변동확률의 합이 1이라면, 모든 변동확률을 절반으로 계산하여 남은 확률에 맞춥니다.
                if (rootFlexible != null)
                {
                    result = rootFlexible.RateCheck(rateLeft, (1.0f - rateNonFlexible) / rateFlexible, out rateLeft);
                };

                //해당하는 아이템이 나온 경우
                if (result != null)
                {
                    //대상이 더 이상 뽑을 수 없는 상태가 된 경우 변동확률 총합을 줄입니다.
                    if (!result.AvailableCheck())
                    {
                        rateFlexible -= result.GetRate();
                    };

                    //대상의 이름을 반환합니다.
                    return result.GetName();
                }
                //해당하는 아이템이 없다면, null을 반환합니다.
                else
                {
                    return null;
                };
            };
        }

        ///<summary>원하는 아이템의 이름을 넣으면, 그 아이템의 정보를 가진 뷰어를 반환합니다.</summary>
        public ContentViewer GetViewer(string name)
        {
            //해당 아이템을 찾습니다.
            Content origin = rootTrie.Find(name);

            //해당 아이템이 존재하면 뷰어를 가져오고, 없으면 null을 반환합니다.
            if (origin != null)
            {
                return origin.GetViewer();
            }
            else
            {
                return null;
            };
        }

        ///<summary>변동/부동확률의 모든 아이템을 보여주는 뷰어 리스트를 반환합니다.</summary>
        ///<param name="wantFlexible">true는 변동, false는 부동확률을 나타냅니다.</param>
        public List<ContentViewer> GetViewerAll(bool wantFlexible)
        {
            //변동확률인지 부동확률인지를 확인한 후에, 모든 내용을 가져옵니다.
            if (wantFlexible)
            {
                if (rootFlexible != null)
                {
                    return rootFlexible.GetViewerAll();
                }
                else
                {
                    return null;
                };
            }
            else
            {
                if (rootNonFlexible != null)
                {
                    return rootNonFlexible.GetViewerAll();
                }
                else
                {
                    return null;
                };
            };
        }

        ///<summary>원하는 이름의 아이템을 삭제합니다.</summary>
        public void Delete(string name)
        {
            //해당하는 아이템을 찾습니다.
            Content targetContent = rootTrie.Find(name);

            //아이템이 존재하는 경우에만 실행합니다.
            if (targetContent != null)
            {
                //Trie 자료구조에서 해당 이름을 삭제합니다.
                targetContent.inTrie.Delete();

                //변동/부동 확률인지 확인한 후에, 해당하는 위치에서 제거하고 확률을 다시 계산합니다.
                if (targetContent.isFlexible)
                {
                    rootFlexible = targetContent.Delete(rootFlexible);
                    rateFlexible = rootFlexible.GetTotalRate();
                }
                else
                {
                    rootNonFlexible = targetContent.Delete(rootNonFlexible);
                    rateNonFlexible = rootNonFlexible.GetTotalRate();
                };
            };
        }

        ///<summary>원하는 아이템의 이름, 확률, 변동가능성을 설정하여 생성합니다. 아이템은 무한히 뽑을 수 있습니다.</summary>
        public bool Insert(string name, float wantRate, bool wantFlexible = false)
        {
            //해당하는 이름의 아이템이 있는지 확인합니다.
            Content targetItem = rootTrie.Find(name);

            wantRate /= 100;
            //없는지 확인이 된 경우에는 새로운 아이템을 생성합니다.
            if (targetItem == null)
            {
                targetItem = new Content(name, wantRate, wantFlexible);

                //변동가능성을 확인하고 해당하는 라인에 넣습니다.
                //이후 확률을 다시 계산합니다.
                if (wantFlexible)
                {
                    rootFlexible = targetItem.Insert(rootFlexible);
                    rateFlexible = rootFlexible.GetTotalRate();
                }
                else
                {
                    rootNonFlexible = targetItem.Insert(rootNonFlexible);
                    rateNonFlexible = rootNonFlexible.GetTotalRate();
                };

                //Trie 자료구조에 해당 이름을 추가합니다.
                rootTrie.Insert(name, targetItem);
                return true;
            }
            //이미 있는 아이템이면 실행하지 않습니다.
            else
            {
                return false;
            };
        }

        ///<summary>원하는 아이템의 이름, 확률, 개수, 변동가능성을 설정하여 생성합니다. 아이템은 개수만큼만 뽑을 수 있습니다.</summary>
        public bool Insert(string name, float wantRate, uint wantNumber, bool wantFlexible)
        {
            //해당하는 이름의 아이템이 있는지 확인합니다.
            Content targetItem = rootTrie.Find(name);

            //없는지 확인이 된 경우에는 새로운 아이템을 생성합니다.
            if (targetItem == null)
            {
                targetItem = new Content(name, wantRate, wantNumber, wantFlexible);

                //변동가능성을 확인하고 해당하는 라인에 넣습니다.
                //이후 확률을 다시 계산합니다.
                if (wantFlexible)
                {
                    rootFlexible = targetItem.Insert(rootFlexible);
                    rateFlexible = rootFlexible.GetTotalRate();
                }
                else
                {
                    rootNonFlexible = targetItem.Insert(rootNonFlexible);
                    rateNonFlexible = rootNonFlexible.GetTotalRate();
                };

                //Trie 자료구조에 해당 이름을 추가합니다.
                rootTrie.Insert(name, targetItem);
                return true;
            }
            //이미 있는 아이템이면 실행하지 않습니다.
            else
            {
                return false;
            };
        }

        ///<summary>원하는 아이템의 개수를 일정량 증가시킵니다.</summary>
        public void NumberAdd(string name, uint wantNumber)
        {
            //해당하는 이름의 아이템이 있는지 확인합니다.
            Content targetContent = rootTrie.Find(name);

            //발견한 아이템이 있는 경우
            if (targetContent != null)
            {
                //원래 뽑을 수 있는 상태였는지 확인합니다.
                bool alreadyAvailable = targetContent.AvailableCheck();
                //아이템 개수를 추가합니다.
                targetContent.AddNumber(wantNumber);

                //만약 원래 뽑을 수 있는 상태가 아니었지만, 개수 추가로 뽑을 수 있게 된 상황이라면
                //해당하는 라인의 확률총합에 현재 확률을 더합니다.
                if (alreadyAvailable == false && targetContent.AvailableCheck())
                {
                    if (targetContent.isFlexible)
                    {
                        rateFlexible += targetContent.GetRate();
                    }
                    else
                    {
                        rateNonFlexible += targetContent.GetRate();
                    };
                };
            };
        }

        ///<summary>원하는 아이템의 개수를 일정량 감소시킵니다.</summary>
        public void NumberSub(string name, uint wantNumber)
        {
            //해당하는 이름의 아이템이 있는지 확인합니다.
            Content targetContent = rootTrie.Find(name);

            //발견한 아이템이 있는 경우
            if (targetContent != null)
            {
                //원래 뽑을 수 있는 상태였는지 확인합니다.
                bool alreadyAvailable = targetContent.AvailableCheck();
                //아이템 개수를 감소시킵니다.
                targetContent.SubNumber(wantNumber);

                //만약 원래 뽑을 수 있는 상태였지만, 개수 감소로 뽑을 수 없게 된 상황이라면
                //해당하는 라인의 확률총합에 현재 확률을 뺍니다.
                if (alreadyAvailable == true && !targetContent.AvailableCheck())
                {
                    if (targetContent.isFlexible)
                    {
                        rateFlexible -= targetContent.GetRate();
                    }
                    else
                    {
                        rateNonFlexible -= targetContent.GetRate();
                    };
                };
            };
        }

        ///<summary>원하는 아이템의 개수를 원하는 개수로 변경합니다.</summary>
        public void SetNumber(string name, uint wantNumber)
        {
            //해당하는 이름의 아이템이 있는지 확인합니다.
            Content targetContent = rootTrie.Find(name);

            //발견한 아이템이 있는 경우
            if (targetContent != null)
            {
                //원래 뽑을 수 있는 상태였는지 확인합니다.
                bool alreadyAvailable = targetContent.AvailableCheck();
                //아이템 개수를 변경합니다.
                targetContent.SetNumber(wantNumber);

                //만약 원래 뽑을 수 있는 상태가 아니었지만, 개수 추가로 뽑을 수 있게 된 상황이라면
                //해당하는 라인의 확률총합에 현재 확률을 더합니다.
                if (alreadyAvailable == false && targetContent.AvailableCheck())
                {
                    if (targetContent.isFlexible)
                    {
                        rateFlexible += targetContent.GetRate();
                    }
                    else
                    {
                        rateNonFlexible += targetContent.GetRate();
                    };
                }
                //만약 원래 뽑을 수 있는 상태였지만, 개수 감소로 뽑을 수 없게 된 상황이라면
                //해당하는 라인의 확률총합에 현재 확률을 뺍니다.
                else if (alreadyAvailable == true && !targetContent.AvailableCheck())
                {
                    if (targetContent.isFlexible)
                    {
                        rateFlexible -= targetContent.GetRate();
                    }
                    else
                    {
                        rateNonFlexible -= targetContent.GetRate();
                    };
                };
            };
        }

        ///<summary>원하는 아이템을 무한정 뽑을 수 있는지 정합니다.</summary>
        public void SetInfinity(string name, bool wantInfinity)
        {
            //해당하는 이름의 아이템이 있는지 확인합니다.
            Content targetContent = rootTrie.Find(name);

            //발견한 아이템이 있는 경우
            if (targetContent != null)
            {
                //원래 뽑을 수 있는 상태였는지 확인합니다.
                bool alreadyAvailable = targetContent.AvailableCheck();

                //무한 여부를 설정합니다.
                targetContent.SetInfinity(wantInfinity);

                //만약 원래 뽑을 수 있는 상태가 아니었지만, 무한정으로 뽑을 수 있게 된 상황이라면
                //해당하는 라인의 확률총합에 현재 확률을 더합니다.
                if (alreadyAvailable == false && targetContent.AvailableCheck())
                {
                    if (targetContent.isFlexible)
                    {
                        rateFlexible += targetContent.GetRate();
                    }
                    else
                    {
                        rateNonFlexible += targetContent.GetRate();
                    };
                }
                //만약 원래 뽑을 수 있는 상태였지만, 무한정 뽑을 수 없게 된 상황이라면
                //해당하는 라인의 확률총합에 현재 확률을 뺍니다.
                else if (alreadyAvailable == true && !targetContent.AvailableCheck())
                {
                    if (targetContent.isFlexible)
                    {
                        rateFlexible -= targetContent.GetRate();
                    }
                    else
                    {
                        rateNonFlexible -= targetContent.GetRate();
                    };
                };
            };
        }

        ///<summary>원하는 아이템의 이름을 from에서 to로 변경합니다.</summary>
        public void SetName(string from, string to)
        {
            //해당하는 아이템이 있는지 확인합니다.
            Content targetContent = rootTrie.Find(from);

            //해당하는 아이템이 있고, to에 해당하는 이름을 가진 아이템이 없다면
            if (targetContent != null && rootTrie.Find(to) == null)
            {
                //해당 아이템의 이름을 변경합니다.
                targetContent.SetName(to, rootTrie);
            };
        }

        ///<summary>원하는 아이템의 확률을 변경합니다.</summary>
        public void SetRate(string name, float wantRate)
        {
            //해당하는 아이템이 있는지 확인합니다.
            Content targetContent = rootTrie.Find(name);

            //발견한 아이템이 있는 경우
            if (targetContent != null)
            {
                //확률을 변경한 후에, 모든 확률을 다시 계산합니다.
                if (targetContent.isFlexible)
                {
                    rootFlexible = targetContent.SetRate(wantRate, rootFlexible);
                    rateFlexible = rootFlexible.GetTotalRate();
                }
                else
                {
                    rootNonFlexible = targetContent.SetRate(wantRate, rootNonFlexible);
                    rateNonFlexible = rootNonFlexible.GetTotalRate();
                };
            };
        }

        ///<summary>원하는 아이템의 변동 가능성을 변경합니다.</summary>
        public void SetFlexible(string name, bool wantFlexible)
        {
            //해당하는 아이템이 있는지 확인합니다.
            Content targetContent = rootTrie.Find(name);

            //발견한 아이템이 있는 경우
            if (targetContent != null)
            {
                //현재 변동 가능성과 입력값이 다르면 실행합니다.
                if (targetContent.isFlexible != wantFlexible)
                {
                    //원래 위치에서 해당하는 아이템을 제거하고 확률을 다시 계산합니다.
                    if (targetContent.isFlexible)
                    {
                        rootFlexible = targetContent.Delete(rootFlexible);
                        rateFlexible = rootFlexible.GetTotalRate();
                    }
                    else
                    {
                        rootNonFlexible = targetContent.Delete(rootNonFlexible);
                        rateNonFlexible = rootNonFlexible.GetTotalRate();
                    };

                    //변동가능성을 대입한 후에, 새로운 위치에 아이템을 추가하고 확률을 다시 계산합니다.
                    targetContent.isFlexible = wantFlexible;
                    if (wantFlexible)
                    {
                        rootFlexible = targetContent.Insert(rootFlexible);
                        rateFlexible = rootFlexible.GetTotalRate();
                    }
                    else
                    {
                        rootNonFlexible = targetContent.Insert(rootNonFlexible);
                        rateNonFlexible = rootNonFlexible.GetTotalRate();
                    };
                };
            };
        }

        ///<summary>원하는 아이템의 활성화 여부를 결정합니다.</summary>
        public void SetAvailable(string name, bool wantAvailable)
        {
            //해당하는 아이템이 있는지 확인합니다.
            Content targetContent = rootTrie.Find(name);

            //발견한 아이템이 있는 경우
            if (targetContent != null)
            {
                //원래 뽑을 수 있는 상태였는지 확인합니다.
                bool beforeAvailable = targetContent.AvailableCheck();

                //활성화 여부를 입력한 값으로 설정합니다.
                targetContent.isAvailable = wantAvailable;

                //이제 뽑을 수 있는 상태인지 확인합니다.
                bool afterAvailable = targetContent.AvailableCheck();

                //뽑을 가능성이 변경된 경우
                if (beforeAvailable != afterAvailable)
                {
                    //이제 가능하면 확률을 추가, 불가능해졌으면 확률을 감소시킵니다.
                    if (afterAvailable)
                    {
                        if (targetContent.isFlexible)
                        {
                            rateFlexible += targetContent.GetRate();
                        }
                        else
                        {
                            rateNonFlexible += targetContent.GetRate();
                        };
                    }
                    else
                    {
                        if (targetContent.isFlexible)
                        {
                            rateFlexible -= targetContent.GetRate();
                        }
                        else
                        {
                            rateNonFlexible -= targetContent.GetRate();
                        };
                    };
                };
            };
        }

        ///<summary>다른 기능이 없이 순수하게 아이템의 내용만을 보여주는 뷰어입니다.</summary>
        public class ContentViewer
        {
            public string name;
            public float rate;
            public uint number;
            public uint pickedTime;
            public bool isInfinity;
            public bool isFlexible;
        }

        ///<summary>랜덤박스에 들어가는 아이템입니다.</summary>
        private class Content
        {
            ///<summary>해당 아이템을 가리키는 Trie노드를 나타냅니다.</summary>
            public TrieNode inTrie;

            ///<summary>해당 아이템의 이전, 이후 아이템을 나타냅니다.</summary>
            private Content next;
            private Content prev;

            ///<summary>해당 아이템의 이름입니다.</summary>
            private string name;
            ///<summary>해당 아이템의 확률입니다.</summary>
            private float rate;
            ///<summary>해당 아이템의 개수입니다.</summary>
            private uint number;

            ///<summary>해당 아이템이 몇 번 뽑혔는지 나타냅니다.</summary>
            private uint pickedTime;

            ///<summary>해당 아이템이 무한히 뽑을 수 있는지 나타냅니다.</summary>
            private bool isInfinity;
            ///<summary>해당 아이템이 변동확률인지 나타냅니다.</summary>
            public bool isFlexible;
            ///<summary>해당 아이템이 활성화되어있는지 나타냅니다.</summary>
            public bool isAvailable;

            ///<summary>이름과 확률, 변동가능성만으로 무한히 뽑을 수 있는 아이템을 만듭니다.</summary>
            public Content(string wantName, float wantRate, bool wantFlexible)
            {
                inTrie = null;

                next = null;
                prev = null;

                name = wantName;
                rate = wantRate;
                number = 0;

                pickedTime = 0;

                isInfinity = true;
                isFlexible = wantFlexible;
                isAvailable = true;
            }

            ///<summary>이름과 확률, 개수, 변동가능성으로 원하는 만큼만 뽑을 수 있는 아이템을 만듭니다.</summary>
            public Content(string wantName, float wantRate, uint wantNumber, bool wantFlexible)
            {
                inTrie = null;

                next = null;
                prev = null;

                name = wantName;
                rate = wantRate;
                number = wantNumber;

                pickedTime = 0;

                isInfinity = false;
                isFlexible = wantFlexible;
                isAvailable = true;
            }

            ~Content()
            {
                if (inTrie != null)
                {
                    inTrie.Delete();
                };
                if (next != null || prev != null)
                {
                    Delete(null);
                };
            }

            ///<summary>현재 아이템이 뽑을 수 있는 상태인지 확인합니다.</summary>
            public bool AvailableCheck()
            {
                //비활성화되어있으면 뽑을 수 없습니다.
                if (!isAvailable)
                {
                    return false;
                }
                //무한이라면 뽑을 수 있습니다.
                else if (isInfinity)
                {
                    return true;
                }
                //개수가 남아있으면 뽑을 수 있습니다.
                else if (number > 0)
                {
                    return true;
                }
                //무한이 아니고 개수가 남지 않았다면 뽑을 수 없습니다.
                else
                {
                    return false;
                };
            }

            ///<summary>확률 확인을 통해 뽑을 수 있는지 확인합니다. 본인이 해당하는 아이템이라면, 본인의 개수를 줄이고 본인을 반환합니다.</summary>
            ///<param name="rateIn">남은 확률을 여기에 넣습니다.</param>
            ///<param name="rateOut">이 아이템을 지나고 난 확률을 반환하는 변수입니다.</param>
            ///<param name="multiplier">변동확률을 위해 존재하는 변수입니다. 아이템에 해당 변수를 곱해서 조정된 확률로 계산하게 합니다.</param>
            public Content RateCheck(float rateIn, float multiplier, out float rateOut)
            {
                //뽑기가 가능한 경우에 실행합니다.
                if (AvailableCheck())
                {
                    //확률에 조정값을 곱합니다.
                    float multiplierRateCurrent = rate * multiplier;

                    //만약 들어온 확률이 내가 가진 확률보다 같거나 작은 경우, 본인이라고 선언합니다.
                    if (rateIn <= multiplierRateCurrent)
                    {
                        //무한이 아닌 경우에는 개수를 줄입니다.
                        if (!isInfinity)
                        {
                            --number;
                        };
                        //뽑힌 횟수를 1 추가합니다.
                        ++pickedTime;

                        //남은 확률이 0이라고 알립니다.
                        rateOut = 0f;

                        //본인을 반환합니다.
                        return this;
                    }
                    //들어온 확률이 내가 가진 확률보다 큰 경우 다음 노드에게 확인을 요청합니다.
                    else
                    {
                        //다음 노드가 있는 경우
                        if (next != null)
                        {
                            //내 확률을 뺀 남은 확률을 다음 노드에 전달합니다.
                            return next.RateCheck(rateIn - multiplierRateCurrent, multiplier, out rateOut);
                        }
                        //다음 노드가 없는 경우
                        else
                        {
                            //남은 확률에 본인 확률을 뺀 나머지를 전해줍니다.
                            rateOut = rateIn - multiplierRateCurrent;

                            //적합한 아이템이 없다고 알립니다.
                            return null;
                        };
                    };
                }
                //뽑기가 불가능한 경우에
                else
                {
                    //다음 노드가 있으면 입력값을 그대로 전달합니다.
                    if (next != null)
                    {
                        return next.RateCheck(rateIn, multiplier, out rateOut);
                    }
                    //다음 노드가 없으면 들어온 확률을 그대로 반환하고, 적합한 아이템이 없다고 알립니다.
                    else
                    {
                        rateOut = rateIn;
                        return null;
                    };
                };
            }

            ///<summary>현재 아이템을 원하는 아이템 라인에 넣습니다. 첫 번째 요소를 넣고 실행하면, 변경된 첫 번째 요소를 반환합니다.</summary>
            public Content Insert(Content targetRoot)
            {
                //첫 번째 요소가 없으면, 본인이 첫 번째라고 알립니다.
                if (targetRoot == null)
                {
                    return this;
                }
                else
                {
                    //첫 번째 요소부터 순차적으로 확인을 시작합니다.
                    Content nextContent = targetRoot;
                    while (nextContent != null)
                    {
                        //확인하는 대상이 본인보다 큰 확률을 가지고 있는 경우
                        if (nextContent.rate > rate)
                        {
                            //대상보다 더 작은 확률이 없을 때
                            if (nextContent.next == null)
                            {
                                //대상의 뒤에 본인을 위치시킨 후, 첫 번째 요소가 그대로 있다고 알립니다.
                                nextContent.next = this;
                                prev = nextContent;
                                return targetRoot;
                            }
                            //대상보다 더 작은 확률이 있는 경우 그 확률을 확인합니다.
                            else
                            {
                                nextContent = nextContent.next;
                            };
                        }
                        //확인하는 대상이 본인보다 작은 확률을 가지고 있는 경우
                        else
                        {
                            //대상보다 큰 확률이 있을 때
                            if (nextContent.prev != null)
                            {
                                //둘 사이에 끼기 위해 앞 뒤를 조정합니다.
                                prev = nextContent.prev;
                                next = nextContent;

                                //둘에게도 본인이 끼었다는 것을 알립니다.
                                nextContent.prev.next = this;
                                nextContent.prev = this;

                                //첫 번째 요소는 그대로 있다고 알립니다.
                                return targetRoot;
                            }
                            //대상보다 큰 확률이 없을 때
                            else
                            {
                                //대상의 앞에 위치한 뒤에, 첫 번째 요소가 본인이 되었다고 알립니다.
                                next = nextContent;
                                nextContent.prev = this;
                                return this;
                            };
                        };
                    };
                    //모든 것을 돌고 난 후에 결정이 되지 않았다면, 첫 번째 요소를 그대로 반환합니다.
                    return targetRoot;
                };
            }

            ///<summary>현재 아이템을 삭제합니다.</summary>
            public Content Delete(Content targetRoot)
            {
                //next를 끊기 전에 미리 저장해둡니다.
                Content saveNext = next;

                //뒷 노드와 연결을 끊습니다.
                if (next != null)
                {
                    next.prev = prev;
                    next = null;
                };

                //앞 노드와 연결을 끊습니다.
                if (prev != null)
                {
                    prev.next = next;
                    prev = null;
                    return targetRoot;
                }
                //앞 노드가 없었다면, 본인이 첫 번째 요소였으므로, 다음 노드가 첫 번째라고 알립니다.
                else
                {
                    return saveNext;
                };
            }

            /// <summary>뒤의 노드와 연결을 끊습니다.</summary>
            public void Destroy()
            {
                prev = null;
                next.Destroy();
                next = null;
                inTrie = null;
            }

            /// <summary>앞, 뒤 노드와 연결을 끊습니다.</summary>
            private void Pop()
            {
                if (next != null)
                {
                    next.prev = prev;
                    next = null;
                };
                if (prev != null)
                {
                    prev.next = next;
                    prev = null;
                };
            }

            /// <summary>해당 노드의 뒤로 이동합니다.</summary>
            private void MoveAfter(Content targetContent)
            {
                //현재 있는 위치의 연결을 끊습니다.
                Pop();

                //앞 뒤 노드를 맞춥니다.
                next = targetContent.next;
                prev = targetContent;

                //앞 뒤 노드에게 본인이 연결되었음을 알립니다.
                if (next != null)
                {
                    next.prev = this;
                };
                targetContent.next = this;
            }

            /// <summary>해당 노드의 앞으로 이동합니다.</summary>
            private void MoveBefore(Content targetContent)
            {
                //현재 있는 위치의 연결을 끊습니다.
                Pop();

                //앞 뒤 노드를 맞춥니다.
                prev = targetContent.prev;
                next = targetContent;

                //앞 뒤 노드에게 본인이 연결되었음을 알립니다.
                if (prev != null)
                {
                    prev.next = this;
                };
                targetContent.prev = this;
            }

            /// <summary>개수를 추가합니다.</summary>
            public void AddNumber(uint wantNumber)
            {
                //대상의 값과 현재 값을 더했을 때, uint의 최댓값을 넘는다면 최댓값으로 고정합니다.
                if (uint.MaxValue - wantNumber <= number)
                {
                    number = uint.MaxValue;
                }
                //아니면 그냥 더합니다.
                else
                {
                    number += wantNumber;
                };
            }

            /// <summary>개수를 감소시킵니다.</summary>
            public void SubNumber(uint wantNumber)
            {
                //감소할 값이 현재 값보다 큰 경우에 0으로 고정합니다.
                if (wantNumber >= number)
                {
                    number = 0;
                }
                //아니면 그냥 뺍니다.
                else
                {
                    number -= wantNumber;
                };
            }

            /// <summary>개수를 설정합니다.</summary>
            public void SetNumber(uint wantNumber)
            {
                number = wantNumber;
            }

            /// <summary>무한히 뽑을 수 있는지 설정합니다.</summary>
            public void SetInfinity(bool wantInfinity)
            {
                isInfinity = wantInfinity;
            }

            /// <summary>이름을 설정합니다.</summary>
            public void SetName(string wantName, TrieNode targetRootTrie)
            {
                //바꿀 이름과 현재 이름이 다른 경우에만 실행합니다.
                if (name != wantName)
                {
                    //현재 연결된 이름을 Trie 자료구조에서 삭제합니다.
                    inTrie.Delete();

                    //새로 변경될 이름을 Trie 자료구조에 추가합니다.
                    targetRootTrie.Insert(wantName, this);

                    //이름을 최종적으로 변경합니다.
                    name = wantName;
                };
            }

            /// <summary>확률을 변경합니다.</summary>
            public Content SetRate(float wantRate, Content targetRoot)
            {
                //확인할 대상을 미리 선언해둡니다.
                Content targetContent;

                //확률이 더 커진 경우
                if (wantRate > rate)
                {
                    //확률을 일단 변경시킵니다.
                    rate = wantRate;

                    //확인할 대상을 자신의 바로 앞으로 설정합니다.
                    targetContent = prev;
                    while (targetContent != null)
                    {
                        //만약, 확인할 대상의 확률이 바뀐 확률보다 크다면, 그 뒤에 놓습니다.
                        if (targetContent.rate > wantRate)
                        {
                            MoveAfter(targetContent);
                            return targetRoot;
                        }
                        //확인할 대상의 확률이 바뀐 확률보다 작다면
                        else
                        {
                            //그보다 큰 확률이 나오는지 확인하기 위해 앞으로 이동합니다.
                            if (targetContent.prev != null)
                            {
                                targetContent = targetContent.prev;
                            }
                            //그보다 큰 확률이 없는 경우, 앞으로 이동하고 맨 앞이 본인이라고 알립니다.
                            else
                            {
                                Pop();
                                next = targetContent;
                                prev = null;
                                targetContent.prev = this;
                                return this;
                            };
                        };
                    };
                }
                //확률이 더 작아지는 경우
                else if (wantRate < rate)
                {
                    //본인이 첫 번째 요소였다면
                    if (targetRoot == this)
                    {
                        //첫 번째 요소를 내 다음 노드로 세팅해두고 시작합니다.
                        targetRoot = next;
                    };

                    //확률을 설정합니다.
                    rate = wantRate;

                    //확인할 대상을 뒷 노드로 설정합니다.
                    targetContent = next;
                    while (targetContent != null)
                    {
                        //확인할 노드의 확률이 변경된 확률보다 작은 경우
                        if (targetContent.rate < wantRate)
                        {
                            //해당 노드의 앞에 위치합니다.
                            MoveBefore(targetContent);

                            //위치한 뒤에, 이 노드보다 큰 확률을 가진 노드가 없으면
                            //이 노드가 첫 번째 요소라고 알립니다.
                            if (prev == null)
                            {
                                return this;
                            }
                            //아니라면, 설정된 첫 번째 요소를 그대로 반환합니다.
                            else
                            {
                                return targetRoot;
                            };
                        }
                        //확인할 노드의 확률이 변경된 확률보다 큰 경우
                        else
                        {
                            //그 보다 작은 확률의 노드가 있다면 해당 노드를 탐색합니다. 
                            if (targetContent.next != null)
                            {
                                targetContent = targetContent.next;
                            }
                            //더 작은 확률의 노드가 없다면, 맨 끝에 위치시키고 설정한 첫 번째 요소를 반환합니다.
                            else
                            {
                                Pop();
                                prev = targetContent;
                                next = null;
                                targetContent.next = this;

                                return targetRoot;
                            };
                        };
                    };
                };
                //모든 것을 거치고 여기에 왔다면, 원래 첫 번째 요소를 그대로 반환합니다.
                return targetRoot;
            }

            /// <summary>현재 개수를 확인합니다.</summary>
            public uint GetNumber()
            {
                return number;
            }

            /// <summary>현재 이름을 확인합니다.</summary>
            public string GetName()
            {
                return name;
            }

            /// <summary>현재 무한한지 확인합니다.</summary>
            public bool GetInfinity()
            {
                return isInfinity;
            }

            /// <summary>현재 확률을 확인합니다.</summary>
            public float GetRate()
            {
                return rate;
            }

            /// <summary>이 노드의 뒤에 이어진 모든 확률을 더해줍니다.</summary>
            public float GetTotalRate()
            {
                //다음 노드가 있을 때
                if (next != null)
                {
                    //이 노드가 뽑을 수 있는 상태일 때는 이 노드의 확률을 더하고, 아니라면 더하지 않습니다.
                    if (AvailableCheck())
                    {
                        return next.GetTotalRate() + rate;
                    }
                    else
                    {
                        return next.GetTotalRate();
                    };

                }
                //다음 노드가 없다면
                else
                {
                    //뽑을 수 있는 상태인지 확인한 후에, 확률을 반환할지 0을 반환할지 고릅니다.
                    if (AvailableCheck())
                    {
                        return rate;
                    }
                    else
                    {
                        return 0;
                    };
                };
            }

            /// <summary>뒤 노드를 반환합니다.</summary>
            public Content GetNext()
            {
                return next;
            }

            /// <summary>앞 노드를 반환합니다.</summary>
            public Content GetPrev()
            {
                return prev;
            }

            /// <summary>이 노드의 정보를 담은 새로운 뷰어를 만듭니다.</summary>
            public ContentViewer GetViewer()
            {
                return new ContentViewer()
                {
                    isFlexible = this.isFlexible,
                    isInfinity = this.isInfinity,
                    name = this.name,
                    number = this.number,
                    rate = this.rate,
                    pickedTime = this.pickedTime
                };
            }

            /// <summary>이 노드와 뒤에 연결된 모든 노드를 보여주는 뷰어를 리스트 형태로 반환합니다.</summary>
            public List<ContentViewer> GetViewerAll()
            {
                List<ContentViewer> returnList = new List<ContentViewer>();

                returnList.Add(GetViewer());

                Content nextContent = next;
                while (nextContent != null)
                {
                    returnList.Add(nextContent.GetViewer());
                    nextContent = nextContent.next;
                };

                return returnList;
            }

        }

        /// <summary>이름을 저장하고, 아이템에 연결시켜주는 자료구조입니다.</summary>
        private class TrieNode
        {
            /// <summary>현재 위치가 나타내는 문자를 보여줍니다.</summary>
            private char character;
            /// <summary>현재 위치와 연결되어 있는 아이템을 보여줍니다.</summary>
            private Content address;

            /// <summary>이 위치에 연결된 상위 위치를 보여줍니다.</summary>
            private TrieNode parent;
            /// <summary>이 위치에 연결된 하위 위치를 보여줍니다.</summary>
            private List<TrieNode> childList;

            public TrieNode()
            {
                childList = new List<TrieNode>();
            }

            TrieNode(char wantCharacter)
            {
                character = wantCharacter;
                childList = new List<TrieNode>();
            }

            /// <summary>원하는 문자가 연결되어있는지 확인합니다.</summary>
            public TrieNode CheckChild(char targetCharacter)
            {
                for (int i = 0; i < childList.Count; ++i)
                {
                    if (childList[i].character == targetCharacter)
                    {
                        return childList[i];
                    };
                };

                return null;
            }

            /// <summary>해당 문자열을 자료구조에 추가합니다.</summary>
            public Content Insert(string wantString, Content wantAddress)
            {
                //해당 문자열이 남아있는 경우
                if (wantString.Length > 0)
                {
                    //현재 연결된 문자 중에 문자열의 첫 번째 문자와 같은 것이 있는지 확인합니다.
                    TrieNode nextChild = CheckChild(wantString[0]);

                    //없는 문자였다면 새로 추가합니다.
                    if (nextChild == null)
                    {
                        nextChild = new TrieNode(wantString[0]);
                        childList.Add(nextChild);
                        nextChild.parent = this;
                    };

                    //문자열의 맨 앞을 뗀 후, 밑에 계속 추가해달라고 요청합니다.
                    return nextChild.Insert(wantString.Substring(1), wantAddress);
                }
                //남은 문자가 없는 경우
                else
                {
                    //받은 주소를 이 위치에 연결하고 해당 아이템을 반환합니다.
                    wantAddress.inTrie = this;
                    address = wantAddress;
                    return address;
                };
            }

            /// <summary>해당 자료구조의 모든 내용을 분리시킵니다.</summary>
            public void Destroy()
            {
                //아이템과 연결을 해제합니다.
                if (address != null)
                {
                    if (address.inTrie == this)
                    {
                        address.inTrie = null;
                    };
                    address = null;
                };

                //하위 위치를 확인합니다.
                if (childList != null)
                {
                    //모든 하위 위치에게 분리를 요청합니다.
                    for (int i = 0; i < childList.Count; ++i)
                    {
                        childList[i].Destroy();
                    };
                    //모든 저장된 하위 위치를 제거합니다.
                    childList.Clear();
                };
            }

            /// <summary>해당 문자열을 자료구조에서 삭제합니다.</summary>
            public void Delete()
            {
                //아이템과 연결을 해제합니다.
                if (address != null)
                {
                    if (address.inTrie == this)
                    {
                        address.inTrie = null;
                    };
                    address = null;
                };

                //하위 위치가 없으면 최하단이므로 바로 삭제를 진행합니다.
                if (childList.Count <= 0)
                {
                    //상위 위치에게 삭제를 부탁하기 위해서 상위 위치를 확인합니다.
                    if (parent != null)
                    {
                        //상위 위치에 연결된 노드가 없고, 갈림길 역할도 하지 않는다면 이 문자열을 위해 만들어진 위치로 보고 함께 삭제합니다.
                        if (parent.address == null && parent.childList.Count <= 1)
                        {
                            parent.childList.Clear();
                            parent.Delete();
                        }
                        //위 조건에 맞지 않는다면, 이 위치만 제거합니다.
                        else
                        {
                            parent.childList.Remove(this);
                        };
                    };
                };
            }

            /// <summary>문자열을 받아서 해당하는 위치를 찾습니다.</summary>
            public Content Find(string wantString)
            {
                //찾을 문자가 남아있을 때
                if (wantString.Length > 0)
                {
                    //하위 위치에서 해당 문자가 있는지 확인합니다.
                    TrieNode nextChild = CheckChild(wantString[0]);

                    //있으면 이어서 찾기 위해 하위 위치에게 나머지 문자열을 넘겨줍니다.
                    if (nextChild != null)
                    {
                        return nextChild.Find(wantString.Substring(1));
                    }
                    //다음 문자가 존재하지 않으면, 없는 것으로 보고 null을 반환합니다.
                    else
                    {
                        return null;
                    };
                }
                //더 이상 찾을 문자가 없으면, 이 위치가 가르키는 아이템을 반환합니다.
                else
                {
                    return address;
                };
            }
        }
    }
}
