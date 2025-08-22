using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models;

public class ExhibitVersionCollection(IList<ExhibitVersionModel> items) : Collection<ExhibitVersionModel>(items);