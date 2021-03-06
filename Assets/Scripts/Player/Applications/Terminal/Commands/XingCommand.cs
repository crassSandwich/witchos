using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityAtoms.BaseAtoms;

namespace WitchOS
{
    public class XingCommand : TerminalCommand
    {
        public BoolVariable XingLock;
        public StringVariable XingTarget;

        public override IEnumerator Evaluate (ITerminal term, string[] arguments)
        {
            if (XingLock.Value)
            {
                term.PrintSingleLine("error - xing is already running in another window");
                yield break;
            }

            if (arguments.Length < 2)
            {
                printUsage(term);
                yield break;
            }

            if (!MagicSource.Instance.On)
            {
                term.PrintSingleLine("error - unable to contact the imps unless magic is on");
                yield break;
            }

            string target = String.Join(" ", arguments.Skip(1));

            XingLock.Value = true;

            term.PrintSingleLine($"pointing imps toward {target}... (press ESC to cancel if desired. this may take some time)");

            float timer = UnityEngine.Random.Range(10, 20);
            while (!term.WasInterrupted && timer >= 0)
            {
                yield return null;
                timer -= Time.deltaTime;
            }

            if (!term.WasInterrupted)
            {
                XingTarget.Value = target;
                term.PrintEmptyLine();
                term.PrintSingleLine("done.");
                yield return new WaitForSeconds(.3f);
                term.PrintSingleLine("now entering stability mode.");
                yield return new WaitForSeconds(.5f);
                term.PrintEmptyLine();
                term.PrintSingleLine("press escape at any time to exit and release the imps from their current target.");
            }

            while (!term.WasInterrupted)
            {
                yield return null;
            }

            XingLock.Value = false;
        }

        public override void CleanUpEarly (ITerminal term)
        {
            XingLock.Value = false;
        }
    }
}
